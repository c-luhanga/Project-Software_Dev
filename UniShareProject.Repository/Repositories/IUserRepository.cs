using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IUserRepository
{
    // Auth helper methods (new requirements)
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<int> InsertAsync(User u, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    
    // Legacy methods for backward compatibility with existing services
    Task<User?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork);
    Task<User?> GetByUsernameAsync(string username, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(User user, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(User user, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<User>> GetAllAsync(IUnitOfWork unitOfWork);
}