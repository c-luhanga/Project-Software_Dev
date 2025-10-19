using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;
using UniShareProject.Repository.Abstractions;

namespace UniShareProject.Repository.Implementations;

public class MessageRepository : 
    UniShareProject.Repository.Abstractions.IMessageRepository, 
    UniShareProject.Repository.Repositories.IMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MessageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    #region New Abstractions Interface Implementation

    public async Task<int> InsertAsync(Message msg, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // Insert message using query from MessageQueries
            var messageId = await connection.QuerySingleAsync<int>(MessageQueries.InsertWithSysDate, 
                new { 
                    ConversationID = msg.ConversationID, 
                    SenderID = msg.SenderID, 
                    Content = msg.Content 
                }, 
                transaction);

            // Update conversation's last message and timestamp using query from MessageQueries
            await connection.ExecuteAsync(MessageQueries.UpdateConversationLastMessageWithSysDate, 
                new { 
                    Content = msg.Content, 
                    ConversationID = msg.ConversationID 
                }, 
                transaction);

            await transaction.CommitAsync(ct);
            return messageId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PagedResult<Message>> GetByConversationAsync(int conversationId, PageSpec page, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        // Use query from MessageQueries for count and paged results
        using var multi = await connection.QueryMultipleAsync(MessageQueries.CountAndPageByConversation, 
            new { ConversationId = conversationId, Offset = page.Offset, PageSize = page.PageSize });

        var total = await multi.ReadSingleAsync<int>();
        var messages = await multi.ReadAsync<Message>();

        return new PagedResult<Message>
        {
            Items = messages,
            Total = total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    #endregion

    #region Legacy Interface Implementation (for backward compatibility)

    public async Task<Message?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Message>(MessageQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Message>(MessageQueries.GetByConversationId, new { ConversationId = conversationId }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Message message, IUnitOfWork unitOfWork)
    {
        var messageId = await unitOfWork.Connection.QuerySingleAsync<int>(MessageQueries.Insert, message, unitOfWork.Transaction);
        
        // Update conversation's last message and timestamp
        await unitOfWork.Connection.ExecuteAsync(MessageQueries.UpdateConversationLastMessage, message, unitOfWork.Transaction);
        
        return messageId;
    }

    public async Task<bool> UpdateAsync(Message message, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(MessageQueries.Update, message, unitOfWork.Transaction);
        
        // Handle IsRead separately through MessageReads table
        if (message.IsRead)
        {
            // Check if read record already exists to avoid duplicates
            const string checkReadSql = "SELECT COUNT(1) FROM dbo.MessageReads WHERE MessageID = @MessageID";
            var readExists = await unitOfWork.Connection.QuerySingleAsync<int>(checkReadSql, new { MessageID = message.MessageID }, unitOfWork.Transaction);
            
            if (readExists == 0)
            {
                await unitOfWork.Connection.ExecuteAsync(MessageQueries.InsertRead, 
                    new { MessageID = message.MessageID, UserID = message.SenderID, ReadAt = DateTime.UtcNow }, 
                    unitOfWork.Transaction);
            }
        }
        
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(MessageQueries.Delete, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    #endregion
}