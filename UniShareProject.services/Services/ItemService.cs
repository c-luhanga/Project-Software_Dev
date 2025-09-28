using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;

namespace UniShareProject.services.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ItemService(IItemRepository itemRepository, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _itemRepository = itemRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

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
            await unitOfWork.CommitAsync();
            return success;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}