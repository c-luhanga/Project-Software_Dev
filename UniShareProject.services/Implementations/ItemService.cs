using AutoMapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.Models;
using UniShareProject.services.Interfaces;

namespace UniShareProject.services.Implementations;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IMapper _mapper;

    public ItemService(
        IItemRepository itemRepository, 
        IUnitOfWorkFactory unitOfWorkFactory,
        IMapper mapper)
    {
        _itemRepository = itemRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
        _mapper = mapper;
    }

    // New required methods
    public async Task<ItemDto> CreateAsync(CreateItemRequest req, int sellerId, CancellationToken ct)
    {
        var item = _mapper.Map<Item>(req);
        item.SellerID = sellerId;
        item.StatusID = 1; // Active status
        
        var itemId = await _itemRepository.InsertAsync(item, ct);
        item.ItemID = itemId;
        
        return _mapper.Map<ItemDto>(item);
    }

    public async Task<ItemDto?> GetAsync(int id, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(id, ct);
        return item != null ? _mapper.Map<ItemDto>(item) : null;
    }

    public async Task<PagedResultDto<ItemDto>> SearchAsync(SearchItemsRequest req, CancellationToken ct)
    {
        var pageSpec = req.ToPageSpec();
        var pagedResult = await _itemRepository.SearchAsync(
            req.CategoryId, 
            req.StatusId, 
            req.ConditionId, 
            req.Q, 
            pageSpec, 
            ct);
        
        var itemDtos = _mapper.Map<IEnumerable<ItemDto>>(pagedResult.Items);
        
        return new PagedResultDto<ItemDto>(
            itemDtos,
            pagedResult.Total,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalPages,
            pagedResult.HasNextPage,
            pagedResult.HasPreviousPage
        );
    }

    public async Task<ItemDto> UpdateStatusAsync(int id, byte statusId, int actorId, CancellationToken ct)
    {
        // TODO: Implement seller/admin authorization check
        // For now, we'll update the status and return the updated item
        
        var affectedRows = await _itemRepository.UpdateStatusAsync(id, statusId, ct);
        
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Item with ID {id} not found or could not be updated");
        }
        
        // Get the updated item
        var updatedItem = await _itemRepository.GetByIdAsync(id, ct);
        if (updatedItem == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found after update");
        }
        
        return _mapper.Map<ItemDto>(updatedItem);
    }

    public async Task<ItemDto> RequestPurchaseAsync(int id, int buyerId, CancellationToken ct)
    {
        // First, get the current item to check its status
        var currentItem = await _itemRepository.GetByIdAsync(id, ct);
        if (currentItem == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found");
        }

        // Check if item is available for purchase (status must be Active = 1)
        if (currentItem.StatusID != 1)
        {
            throw new InvalidOperationException($"Item with ID {id} is not available for purchase. Current status: {currentItem.StatusID}");
        }

        // Update status to Pending (2)
        var affectedRows = await _itemRepository.UpdateStatusAsync(id, 2, ct);
        
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Item with ID {id} could not be updated");
        }

        // Get the updated item
        var updatedItem = await _itemRepository.GetByIdAsync(id, ct);
        if (updatedItem == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found after update");
        }

        return _mapper.Map<ItemDto>(updatedItem);
    }

    // Legacy methods for backward compatibility
    public async Task<Item?> GetByIdAsync(int id)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _itemRepository.GetByIdAsync(id, unitOfWork);
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _itemRepository.GetAllAsync(unitOfWork);
    }

    public async Task<IEnumerable<Item>> GetByUserIdAsync(int userId)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _itemRepository.GetByUserIdAsync(userId, unitOfWork);
    }

    public async Task<IEnumerable<Item>> SearchAsync(string query)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        if (string.IsNullOrWhiteSpace(query))
            return await _itemRepository.GetAllAsync(unitOfWork);

        return await _itemRepository.SearchAsync(query, unitOfWork);
    }

    public async Task<Item> CreateAsync(Item item)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        try
        {
            item.Id = await _itemRepository.CreateAsync(item, unitOfWork);
            await unitOfWork.CommitAsync();
            return item;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Item> UpdateAsync(Item item)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        item.UpdatedAt = DateTime.UtcNow;

        try
        {
            var success = await _itemRepository.UpdateAsync(item, unitOfWork);
            if (!success)
                throw new InvalidOperationException("Item not found or could not be updated");

            await unitOfWork.CommitAsync();
            return item;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            var success = await _itemRepository.DeleteAsync(id, unitOfWork);
            if (success)
            {
                await unitOfWork.CommitAsync();
            }
            else
            {
                await unitOfWork.RollbackAsync();
            }
            return success;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}