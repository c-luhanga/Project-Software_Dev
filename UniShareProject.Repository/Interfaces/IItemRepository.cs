using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IItemRepository
{
    // New required methods
    Task<int> InsertAsync(Item e, CancellationToken ct);
    Task<Item?> GetByIdAsync(int id, CancellationToken ct);
    Task<int> UpdateAsync(Item item, CancellationToken ct);
    Task<int> UpdateStatusAsync(int id, byte statusId, CancellationToken ct);
    Task<PagedResult<Item>> SearchAsync(int? categoryId, byte? statusId, byte? conditionId, string? q, PageSpec page, CancellationToken ct);
    Task<(int SellerId, byte StatusId)?> GetSellerAndStatusAsync(int id, CancellationToken ct);
    Task<int> HardDeleteAsync(int id, CancellationToken ct);
    
    // Dashboard statistics methods
    Task<int> GetTotalItemsAsync(CancellationToken ct);
    Task<int> GetActiveItemsCountAsync(CancellationToken ct);
    Task<int> GetPendingItemsCountAsync(CancellationToken ct);
    Task<int> GetSoldItemsCountAsync(CancellationToken ct);
    Task<int> GetWithdrawnItemsCountAsync(CancellationToken ct);

    // Existing methods for backward compatibility
    Task<Item?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> GetAllAsync(IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    Task<IEnumerable<Item>> SearchAsync(string query, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Item item, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Item item, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
}
