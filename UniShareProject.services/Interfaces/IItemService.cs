using UniShareProject.Repository.Models;
using UniShareProject.services.Models;

namespace UniShareProject.services.Interfaces;

public interface IItemService
{
    // New required methods
    Task<ItemDto> CreateAsync(CreateItemRequest req, int sellerId, CancellationToken ct);
    Task<ItemDto?> GetAsync(int id, CancellationToken ct);
    Task<PagedResultDto<ItemDto>> SearchAsync(SearchItemsRequest req, CancellationToken ct);
    Task<ItemDto> UpdateStatusAsync(int id, byte statusId, int actorId, CancellationToken ct);
    Task<ItemDto> RequestPurchaseAsync(int id, int buyerId, CancellationToken ct);
    Task<IReadOnlyList<string>> AddImagesAsync(int itemId, int actorId, AddItemImagesRequest req, CancellationToken ct);
    Task<ItemDto> MarkSoldAsync(int id, int actorId, CancellationToken ct);
    Task<bool> AdminDeleteAsync(int id, int adminId, CancellationToken ct);

    // Legacy methods for backward compatibility
    Task<Item?> GetByIdAsync(int id);
    Task<IEnumerable<Item>> GetAllAsync();
    Task<IEnumerable<Item>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Item>> SearchAsync(string query);
    Task<Item> CreateAsync(Item item);
    Task<Item> UpdateAsync(Item item);
    Task<bool> DeleteAsync(int id);
}
