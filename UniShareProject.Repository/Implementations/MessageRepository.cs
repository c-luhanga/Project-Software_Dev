using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;

namespace UniShareProject.Repository.Implementations;

public class MessageRepository : IMessageRepository
{
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
}