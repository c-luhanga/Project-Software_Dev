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
    private readonly IItemImageRepository _itemImageRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IMapper _mapper;

    public ItemService(
        IItemRepository itemRepository,
        IItemImageRepository itemImageRepository,
        IUnitOfWorkFactory unitOfWorkFactory,
        IMapper mapper)
    {
        _itemRepository = itemRepository;
        _itemImageRepository = itemImageRepository;
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

    public async Task<ItemDto> UpdateAsync(int id, UpdateItemRequest req, int actorId, CancellationToken ct)
    {
        // Get the existing item to verify ownership/permissions
        var existingItem = await _itemRepository.GetByIdAsync(id, ct);
        if (existingItem == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found");
        }

        // TODO: Add authorization check - only item owner or admin can update
        // For now, we'll proceed with the update
        
        // Create updated item with only the fields that were provided
        var updatedItem = new Item
        {
            ItemID = existingItem.ItemID,
            Title = req.Title ?? existingItem.Title,
            Description = req.Description ?? existingItem.Description,
            CategoryID = req.CategoryId ?? existingItem.CategoryID,
            Price = req.Price ?? existingItem.Price,
            ConditionID = req.ConditionId ?? existingItem.ConditionID,
            SellerID = existingItem.SellerID, // Never change seller
            StatusID = existingItem.StatusID, // Don't change status via update
            PostedDate = existingItem.PostedDate // Keep original posted date
        };

        var affectedRows = await _itemRepository.UpdateAsync(updatedItem, ct);
        
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Item with ID {id} could not be updated");
        }

        // Get the updated item with images
        var result = await GetAsync(id, ct);
        if (result == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found after update");
        }

        return result;
    }

    public async Task<ItemDto?> GetAsync(int id, CancellationToken ct)
    {
        var item = await _itemRepository.GetByIdAsync(id, ct);
        if (item == null) return null;

        // Map item to DTO
        var itemDto = _mapper.Map<ItemDto>(item);

        // Load images for this item
        var imageUrls = await _itemImageRepository.GetUrlsAsync(id, ct);
        itemDto.Images = imageUrls.ToList();

        return itemDto;
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
        
        var itemDtos = _mapper.Map<IEnumerable<ItemDto>>(pagedResult.Items).ToList();
        
        // Load images for each item (including thumbnails)
        foreach (var itemDto in itemDtos)
        {
            var imageUrls = await _itemImageRepository.GetUrlsAsync(itemDto.Id, ct);
            itemDto.Images = imageUrls.ToList();
        }
        
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

    public async Task<IReadOnlyList<string>> AddImagesAsync(int itemId, int actorId, AddItemImagesRequest req, CancellationToken ct)
    {
        // Step 1: Fetch item; if not found, throw 404
        var item = await _itemRepository.GetByIdAsync(itemId, ct);
        if (item == null)
        {
            throw new KeyNotFoundException($"Item with ID {itemId} not found");
        }

        // Step 2: Verify actorId == item.SellerId
        if (actorId != item.SellerID)
        {
            throw new UnauthorizedAccessException($"User {actorId} is not authorized to add images to item {itemId}");
        }

        // Step 3: Count existing + new ? 4
        var existingImageCount = await _itemImageRepository.CountByItemAsync(itemId, ct);
        var totalImages = existingImageCount + req.ImageUrls.Count;
        
        if (totalImages > 4)
        {
            throw new InvalidOperationException($"Cannot add {req.ImageUrls.Count} images. Item already has {existingImageCount} images. Maximum allowed is 4 total.");
        }

        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Step 4: Loop insert each URL
            foreach (var url in req.ImageUrls)
            {
                await _itemImageRepository.InsertAsync(itemId, url, ct);
            }

            // Step 5: Commit the transaction
            await unitOfWork.CommitAsync();

            // Step 6: Return all image URLs via GetUrlsAsync
            return await _itemImageRepository.GetUrlsAsync(itemId, ct);
        }
        catch
        {
            // If anything goes wrong, the transaction will be rolled back automatically
            throw;
        }
    }

    public async Task<ItemDto> MarkSoldAsync(int id, int actorId, CancellationToken ct)
    {
        // Step 1: Get (SellerId, StatusId) via repo. If null ? throw KeyNotFoundException
        var itemInfo = await _itemRepository.GetSellerAndStatusAsync(id, ct);
        if (itemInfo == null)
        {
            throw new KeyNotFoundException($"Item with ID {id} not found");
        }

        var (sellerId, statusId) = itemInfo.Value;

        // Step 2: If actorId != SellerId ? throw UnauthorizedAccessException
        if (actorId != sellerId)
        {
            throw new UnauthorizedAccessException($"User {actorId} is not authorized to mark item {id} as sold");
        }

        // Step 3: If StatusId ? 2 (Pending) ? throw InvalidOperationException
        if (statusId != 2)
        {
            throw new InvalidOperationException($"Item {id} cannot be marked as sold. Current status is {statusId}, but it must be Pending (2)");
        }

        // Step 4: Call UpdateStatusAsync(id, 3) (Sold)
        var affectedRows = await _itemRepository.UpdateStatusAsync(id, 3, ct);
        
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Item with ID {id} could not be updated");
        }

        // Step 5: Return updated item via GetByIdAsync ? map to ItemDto
        var updatedItem = await _itemRepository.GetByIdAsync(id, ct);
        if (updatedItem == null)
        {
            throw new InvalidOperationException($"Item with ID {id} not found after update");
        }

        return _mapper.Map<ItemDto>(updatedItem);
    }

    public async Task<bool> AdminDeleteAsync(int id, int adminId, CancellationToken ct)
    {
        // Step 1: Verify the item exists
        var item = await _itemRepository.GetByIdAsync(id, ct);
        if (item == null)
        {
            throw new KeyNotFoundException($"Item with ID {id} not found");
        }

        // Step 2: Perform hard delete (admin bypass - no authorization check needed here as controller handles admin auth)
        var affectedRows = await _itemRepository.HardDeleteAsync(id, ct);
        
        // Step 3: Return success if item was deleted
        return affectedRows > 0;
    }

    public async Task<IEnumerable<ItemDto>> GetMyItemsAsync(int userId, CancellationToken ct)
    {
        // Use the new repository method that supports CancellationToken
        // Get user items from repository
        await using var unitOfWork = _unitOfWorkFactory.Create();
        var items = await _itemRepository.GetByUserIdAsync(userId, unitOfWork);
        
        // Map entities to DTOs using AutoMapper
        var itemDtos = items.Select(item => _mapper.Map<ItemDto>(item)).ToList();
        
        // Load images for each item
        foreach (var itemDto in itemDtos)
        {
            var imageUrls = await _itemImageRepository.GetUrlsAsync(itemDto.Id, ct);
            itemDto.Images = imageUrls.ToList();
        }
        
        return itemDtos;
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