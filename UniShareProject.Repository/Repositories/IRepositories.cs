using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> GetAllAsync(IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> SearchAsync(string query, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Item item, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Item item, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Message message, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Message message, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}