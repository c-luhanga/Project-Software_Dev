using Dapper;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public class MessageRepository : IMessageRepository
{
    public async Task<Message?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            SELECT m.MessageID, m.ConversationID, m.SenderID, m.Content, m.Timestamp,
                   CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
            FROM dbo.Messages m
            LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
            WHERE m.MessageID = @Id";
        
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Message>(sql, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            SELECT m.MessageID, m.ConversationID, m.SenderID, m.Content, m.Timestamp,
                   CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
            FROM dbo.Messages m
            LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
            WHERE m.ConversationID = @ConversationId 
            ORDER BY m.Timestamp";
        
        return await unitOfWork.Connection.QueryAsync<Message>(sql, new { ConversationId = conversationId }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Message message, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            INSERT INTO dbo.Messages (ConversationID, SenderID, Content, Timestamp)
            VALUES (@ConversationID, @SenderID, @Content, @Timestamp);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        var messageId = await unitOfWork.Connection.QuerySingleAsync<int>(sql, message, unitOfWork.Transaction);
        
        // Update conversation's last message and timestamp
        const string updateConversationSql = @"
            UPDATE dbo.Conversations 
            SET LastMessage = @Content, LastUpdated = @Timestamp 
            WHERE ConversationID = @ConversationID";
        
        await unitOfWork.Connection.ExecuteAsync(updateConversationSql, message, unitOfWork.Transaction);
        
        return messageId;
    }

    public async Task<bool> UpdateAsync(Message message, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            UPDATE dbo.Messages 
            SET Content = @Content
            WHERE MessageID = @MessageID";
        
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, message, unitOfWork.Transaction);
        
        // Handle IsRead separately through MessageReads table
        if (message.IsRead)
        {
            await MarkAsReadAsync(message.MessageID, message.SenderID, unitOfWork);
        }
        
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = "DELETE FROM dbo.Messages WHERE MessageID = @Id";
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }
    
    private async Task MarkAsReadAsync(int messageId, int userId, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            INSERT INTO dbo.MessageReads (MessageID, UserID, ReadAt)
            VALUES (@MessageId, @UserId, GETDATE())
            ON CONFLICT (MessageID, UserID) DO NOTHING";
        
        // SQL Server doesn't have ON CONFLICT, so use MERGE instead
        const string mergeSql = @"
            MERGE dbo.MessageReads AS target
            USING (SELECT @MessageId as MessageID, @UserId as UserID) AS source
            ON target.MessageID = source.MessageID AND target.UserID = source.UserID
            WHEN NOT MATCHED THEN
                INSERT (MessageID, UserID, ReadAt)
                VALUES (source.MessageID, source.UserID, GETDATE());";
        
        await unitOfWork.Connection.ExecuteAsync(mergeSql, new { MessageId = messageId, UserId = userId }, unitOfWork.Transaction);
    }
}