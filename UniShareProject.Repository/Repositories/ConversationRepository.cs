using Dapper;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public class ConversationRepository : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            SELECT c.ConversationID, c.ItemID, c.LastMessage, c.LastUpdated, c.CreatedAt,
                   cp1.UserID as BuyerId, cp2.UserID as SellerId
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp1 ON c.ConversationID = cp1.ConversationID
            INNER JOIN dbo.ConversationParticipants cp2 ON c.ConversationID = cp2.ConversationID
            WHERE c.ConversationID = @Id AND cp1.UserID != cp2.UserID";
        
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Conversation>(sql, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            SELECT DISTINCT c.ConversationID, c.ItemID, c.LastMessage, c.LastUpdated, c.CreatedAt
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp ON c.ConversationID = cp.ConversationID
            WHERE cp.UserID = @UserId
            ORDER BY c.LastUpdated DESC";
        
        return await unitOfWork.Connection.QueryAsync<Conversation>(sql, new { UserId = userId }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Conversation conversation, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            INSERT INTO dbo.Conversations (ItemID, LastMessage, LastUpdated, CreatedAt)
            VALUES (@ItemID, @LastMessage, @LastUpdated, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        var conversationId = await unitOfWork.Connection.QuerySingleAsync<int>(sql, conversation, unitOfWork.Transaction);
        
        // Add participants
        const string participantSql = @"
            INSERT INTO dbo.ConversationParticipants (ConversationID, UserID)
            VALUES (@ConversationId, @UserId)";
        
        await unitOfWork.Connection.ExecuteAsync(participantSql, 
            new { ConversationId = conversationId, UserId = conversation.BuyerId }, 
            unitOfWork.Transaction);
        
        await unitOfWork.Connection.ExecuteAsync(participantSql, 
            new { ConversationId = conversationId, UserId = conversation.SellerId }, 
            unitOfWork.Transaction);
        
        return conversationId;
    }

    public async Task<bool> UpdateAsync(Conversation conversation, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            UPDATE dbo.Conversations 
            SET ItemID = @ItemID, LastMessage = @LastMessage, LastUpdated = @LastUpdated
            WHERE ConversationID = @ConversationID";
        
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, conversation, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        // Delete conversation participants first (due to foreign key constraints)
        const string deleteParticipantsSql = "DELETE FROM dbo.ConversationParticipants WHERE ConversationID = @Id";
        await unitOfWork.Connection.ExecuteAsync(deleteParticipantsSql, new { Id = id }, unitOfWork.Transaction);
        
        // Then delete the conversation
        const string sql = "DELETE FROM dbo.Conversations WHERE ConversationID = @Id";
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }
}