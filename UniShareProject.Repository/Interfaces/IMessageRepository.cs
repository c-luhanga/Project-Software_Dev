using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Message message, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Message message, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}
