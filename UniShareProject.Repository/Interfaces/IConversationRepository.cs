using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}
