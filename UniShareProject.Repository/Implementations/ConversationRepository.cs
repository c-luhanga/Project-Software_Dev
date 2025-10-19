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
            // Find existing conversation using query from ConversationQueries
            var existingConversationId = await connection.QueryFirstOrDefaultAsync<int?>(
                ConversationQueries.FindExistingConversationForEnsure, 
                new { ItemId = itemId, UserA = userA, UserB = userB }, 
                transaction);

            if (existingConversationId.HasValue)
            {
                await transaction.CommitAsync(ct);
                return existingConversationId.Value;
            }

            // Create new conversation using query from ConversationQueries
            var conversationId = await connection.QuerySingleAsync<int>(
                ConversationQueries.InsertWithSysDate, 
                new { ItemId = itemId }, 
                transaction);

            // Insert participants using query from ConversationQueries
            await connection.ExecuteAsync(ConversationQueries.InsertParticipant, 
                new { ConversationId = conversationId, UserId = userA }, transaction);
            
            await connection.ExecuteAsync(ConversationQueries.InsertParticipant, 
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

        // Use query from ConversationQueries
        var count = await connection.QuerySingleAsync<int>(ConversationQueries.CheckUserParticipation, 
            new { ConversationId = conversationId, UserId = userId });

        return count > 0;
    }

    public async Task<PagedResult<ConversationListData>> ListForUserAsync(int userId, PageSpec page, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        // Use query from ConversationQueries for count and paged results
        using var multi = await connection.QueryMultipleAsync(ConversationQueries.CountAndPageConversationsByUserId, 
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