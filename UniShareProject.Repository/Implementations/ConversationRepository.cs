using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;
using UniShareProject.Repository.Abstractions;

namespace UniShareProject.Repository.Implementations;

public class ConversationRepository : 
    UniShareProject.Repository.Abstractions.IConversationRepository, 
    UniShareProject.Repository.Repositories.IConversationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ConversationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    #region New Abstractions Interface Implementation

    public async Task<int> EnsureConversationAsync(int? itemId, int userA, int userB, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // Find existing conversation with same participants and ItemId (including NULL)
            const string findExistingSql = @"
                SELECT TOP 1 c.ConversationID
                FROM dbo.Conversations c
                INNER JOIN dbo.ConversationParticipants cp1 ON c.ConversationID = cp1.ConversationID
                INNER JOIN dbo.ConversationParticipants cp2 ON c.ConversationID = cp2.ConversationID
                WHERE (c.ItemID = @ItemId OR (c.ItemID IS NULL AND @ItemId IS NULL))
                AND cp1.UserID = @UserA 
                AND cp2.UserID = @UserB
                AND cp1.UserID != cp2.UserID";

            var existingConversationId = await connection.QueryFirstOrDefaultAsync<int?>(
                findExistingSql, 
                new { ItemId = itemId, UserA = userA, UserB = userB }, 
                transaction);

            if (existingConversationId.HasValue)
            {
                await transaction.CommitAsync(ct);
                return existingConversationId.Value;
            }

            // Create new conversation
            const string insertConversationSql = @"
                INSERT INTO dbo.Conversations (ItemID, LastMessage, LastUpdated, CreatedAt)
                VALUES (@ItemId, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var conversationId = await connection.QuerySingleAsync<int>(
                insertConversationSql, 
                new { ItemId = itemId }, 
                transaction);

            // Insert participants
            const string insertParticipantSql = @"
                INSERT INTO dbo.ConversationParticipants (ConversationID, UserID)
                VALUES (@ConversationId, @UserId)";

            await connection.ExecuteAsync(insertParticipantSql, 
                new { ConversationId = conversationId, UserId = userA }, transaction);
            
            await connection.ExecuteAsync(insertParticipantSql, 
                new { ConversationId = conversationId, UserId = userB }, transaction);

            await transaction.CommitAsync(ct);
            return conversationId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> UserIsParticipantAsync(int conversationId, int userId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.ConversationParticipants 
            WHERE ConversationID = @ConversationId AND UserID = @UserId";

        var count = await connection.QuerySingleAsync<int>(sql, 
            new { ConversationId = conversationId, UserId = userId });

        return count > 0;
    }

    public async Task<PagedResult<ConversationListData>> ListForUserAsync(int userId, PageSpec page, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string countAndPageSql = @"
            -- Count query
            SELECT COUNT(DISTINCT c.ConversationID)
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp ON c.ConversationID = cp.ConversationID
            WHERE cp.UserID = @UserId;

            -- Paged results query
            SELECT 
                c.ConversationID,
                c.ItemID,
                c.LastMessage,
                c.LastUpdated,
                otherUser.UserID as OtherUserId,
                otherUser.FirstName + ' ' + otherUser.LastName as OtherUserName,
                ISNULL(unread.UnreadCount, 0) as UnreadCount
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp ON c.ConversationID = cp.ConversationID
            INNER JOIN dbo.ConversationParticipants otherCp ON c.ConversationID = otherCp.ConversationID
            INNER JOIN dbo.Users otherUser ON otherCp.UserID = otherUser.UserID
            LEFT JOIN (
                SELECT m.ConversationID, COUNT(*) as UnreadCount
                FROM dbo.Messages m
                LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID AND mr.UserID = @UserId
                WHERE mr.MessageID IS NULL AND m.SenderID != @UserId
                GROUP BY m.ConversationID
            ) unread ON c.ConversationID = unread.ConversationID
            WHERE cp.UserID = @UserId AND otherCp.UserID != @UserId
            ORDER BY c.LastUpdated DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        using var multi = await connection.QueryMultipleAsync(countAndPageSql, 
            new { UserId = userId, Offset = page.Offset, PageSize = page.PageSize });

        var total = await multi.ReadSingleAsync<int>();
        var conversations = await multi.ReadAsync<ConversationListData>();

        return new PagedResult<ConversationListData>
        {
            Items = conversations,
            Total = total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    #endregion

    #region Legacy Interface Implementation (for backward compatibility)

    public async Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Conversation>(ConversationQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Conversation>(ConversationQueries.GetByUserId, new { UserId = userId }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<ConversationListData>> GetConversationListByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<ConversationListData>(ConversationQueries.GetConversationListByUserId, new { UserId = userId }, unitOfWork.Transaction);
    }

    public async Task<bool> IsUserParticipantAsync(int conversationId, int userId, IUnitOfWork unitOfWork)
    {
        var count = await unitOfWork.Connection.QuerySingleAsync<int>(ConversationQueries.CheckUserParticipation, 
            new { ConversationId = conversationId, UserId = userId }, unitOfWork.Transaction);
        return count > 0;
    }

    public async Task<int?> FindExistingConversationAsync(int itemId, int userId1, int userId2, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<int?>(ConversationQueries.FindExistingConversation, 
            new { ItemId = itemId, UserId1 = userId1, UserId2 = userId2 }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Conversation conversation, IUnitOfWork unitOfWork)
    {
        var conversationId = await unitOfWork.Connection.QuerySingleAsync<int>(ConversationQueries.Insert, conversation, unitOfWork.Transaction);
        
        // Add participants
        await unitOfWork.Connection.ExecuteAsync(ConversationQueries.InsertParticipant, 
            new { ConversationId = conversationId, UserId = conversation.BuyerId }, 
            unitOfWork.Transaction);
        
        await unitOfWork.Connection.ExecuteAsync(ConversationQueries.InsertParticipant, 
            new { ConversationId = conversationId, UserId = conversation.SellerId }, 
            unitOfWork.Transaction);
        
        return conversationId;
    }

    public async Task<bool> UpdateAsync(Conversation conversation, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(ConversationQueries.Update, conversation, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(ConversationQueries.Delete, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    #endregion
}