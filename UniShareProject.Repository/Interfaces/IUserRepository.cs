using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using System.Threading;
using System.Threading.Tasks;

namespace UniShareProject.Repository.Repositories;

public interface IUserRepository
{
    // Auth helper methods (now require unit of work)
    Task<bool> EmailExistsAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct);
    Task<int> InsertAsync(User u, IUnitOfWork unitOfWork, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct);
    
    // New admin methods
    Task<bool> UserExistsAsync(int id, CancellationToken ct);
    Task<int> BanUserAsync(int id, CancellationToken ct);
    Task<int> UnbanUserAsync(int id, CancellationToken ct);
    
    // Dashboard statistics methods
    Task<int> GetTotalUsersAsync(CancellationToken ct);
    Task<int> GetBannedUsersCountAsync(CancellationToken ct);
    Task<int> GetAdminUsersCountAsync(CancellationToken ct);
    
    // Legacy methods for backward compatibility with existing services
    Task<User?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork);
    Task<User?> GetByUsernameAsync(string username, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(User user, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(User user, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<User>> GetAllAsync(IUnitOfWork unitOfWork);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<int> UpdateProfileAsync(int userId, string? phone, string? house, string? profileImageUrl, CancellationToken ct);
}