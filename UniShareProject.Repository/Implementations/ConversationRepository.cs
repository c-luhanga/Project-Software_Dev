using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;

namespace UniShareProject.Repository.Implementations;

public class ConversationRepository : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Conversation>(ConversationQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Conversation>(ConversationQueries.GetByUserId, new { UserId = userId }, unitOfWork.Transaction);
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
}