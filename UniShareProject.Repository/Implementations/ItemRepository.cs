using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;
using System.Text;

namespace UniShareProject.Repository.Implementations;

public class ItemRepository : IItemRepository
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ItemRepository(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    // New required methods
    public async Task<int> InsertAsync(Item e, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Avoid Dapper duplicate parameter names by passing only the exact parameters used in SQL
            var p = new DynamicParameters();
            p.Add("Title", e.Title);
            p.Add("Description", e.Description);
            p.Add("CategoryID", e.CategoryID);
            p.Add("Price", e.Price);
            p.Add("ConditionID", e.ConditionID);
            p.Add("StatusID", e.StatusID);
            p.Add("SellerID", e.SellerID);

            var result = await unitOfWork.Connection.QuerySingleAsync<int>(ItemQueries.Insert, p, unitOfWork.Transaction);
            await unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Item?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Item>(ItemQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<int> UpdateAsync(Item item, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            var p = new DynamicParameters();
            p.Add("ItemID", item.ItemID);
            p.Add("Title", item.Title);
            p.Add("Description", item.Description);
            p.Add("CategoryID", item.CategoryID);
            p.Add("Price", item.Price);
            p.Add("ConditionID", item.ConditionID);
            p.Add("StatusID", item.StatusID);

            var affectedRows = await unitOfWork.Connection.ExecuteAsync(ItemQueries.Update, p, unitOfWork.Transaction);
            await unitOfWork.CommitAsync();
            return affectedRows;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<(int SellerId, byte StatusId)?> GetSellerAndStatusAsync(int id, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        var result = await unitOfWork.Connection.QueryFirstOrDefaultAsync<dynamic>(
            ItemQueries.GetSellerAndStatus, 
            new { Id = id }, 
            unitOfWork.Transaction);
        
        if (result == null)
            return null;
        
        return ((int)result.SellerID, (byte)result.StatusID);
    }

    public async Task<int> UpdateStatusAsync(int id, byte statusId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            var affectedRows = await unitOfWork.Connection.QuerySingleAsync<int>(ItemQueries.UpdateStatus, new { Id = id, StatusId = statusId }, unitOfWork.Transaction);
            await unitOfWork.CommitAsync();
            return affectedRows;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<int> HardDeleteAsync(int id, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            var affectedRows = await unitOfWork.Connection.QuerySingleAsync<int>(ItemQueries.HardDelete, new { Id = id }, unitOfWork.Transaction);
            await unitOfWork.CommitAsync();
            return affectedRows;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<Item>> SearchAsync(int? categoryId, byte? statusId, byte? conditionId, string? q, PageSpec page, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        var whereBuilder = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();
        
        // Build WHERE clause dynamically
        if (categoryId.HasValue)
        {
            whereBuilder.Append(" AND i.CategoryID = @CategoryId");
            parameters.Add("CategoryId", categoryId.Value);
        }
        
        if (statusId.HasValue)
        {
            whereBuilder.Append(" AND i.StatusID = @StatusId");
            parameters.Add("StatusId", statusId.Value);
        }
        
        if (conditionId.HasValue)
        {
            whereBuilder.Append(" AND i.ConditionID = @ConditionId");
            parameters.Add("ConditionId", conditionId.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            whereBuilder.Append(" AND (i.Title LIKE @SearchQuery OR i.Description LIKE @SearchQuery)");
            parameters.Add("SearchQuery", $"%{q}%");
        }
        
        var whereClause = whereBuilder.ToString();
        
        // Add pagination parameters
        parameters.Add("Offset", page.Offset);
        parameters.Add("PageSize", page.PageSize);
        
        var finalSql = string.Format(ItemQueries.SearchPaginated, whereClause);
        
        using var multi = await unitOfWork.Connection.QueryMultipleAsync(finalSql, parameters, unitOfWork.Transaction);
        
        var total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<Item>();
        
        return new PagedResult<Item>
        {
            Items = items,
            Total = total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    // Dashboard statistics methods
    public async Task<int> GetTotalItemsAsync(CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemQueries.GetTotalItems,
            transaction: unitOfWork.Transaction
        );
    }

    public async Task<int> GetActiveItemsCountAsync(CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemQueries.GetActiveItemsCount,
            transaction: unitOfWork.Transaction
        );
    }

    public async Task<int> GetPendingItemsCountAsync(CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemQueries.GetPendingItemsCount,
            transaction: unitOfWork.Transaction
        );
    }

    public async Task<int> GetSoldItemsCountAsync(CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemQueries.GetSoldItemsCount,
            transaction: unitOfWork.Transaction
        );
    }

    public async Task<int> GetWithdrawnItemsCountAsync(CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemQueries.GetWithdrawnItemsCount,
            transaction: unitOfWork.Transaction
        );
    }

    public async Task<Item?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Item>(ItemQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Item>(ItemQueries.GetAllAvailable, transaction: unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Item>(ItemQueries.GetByUserId, new { UserId = userId }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> SearchAsync(string query, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<Item>(ItemQueries.SearchLegacy, new { Query = $"%{query}%" }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Item item, IUnitOfWork unitOfWork)
    {
        // Use explicit parameters here as well to avoid duplicate parameter names from mirrored properties
        var p = new DynamicParameters();
        p.Add("Title", item.Title);
        p.Add("Description", item.Description);
        p.Add("CategoryID", item.CategoryID);
        p.Add("Price", item.Price);
        p.Add("ConditionID", item.ConditionID);
        p.Add("StatusID", item.StatusID);
        p.Add("SellerID", item.SellerID);
        p.Add("PostedDate", item.PostedDate);

        return await unitOfWork.Connection.QuerySingleAsync<int>(ItemQueries.InsertLegacy, p, unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(Item item, IUnitOfWork unitOfWork)
    {
        var p = new DynamicParameters();
        p.Add("ItemID", item.ItemID);
        p.Add("Title", item.Title);
        p.Add("Description", item.Description);
        p.Add("CategoryID", item.CategoryID);
        p.Add("Price", item.Price);
        p.Add("ConditionID", item.ConditionID);
        p.Add("StatusID", item.StatusID);

        var affectedRows = await unitOfWork.Connection.ExecuteAsync(ItemQueries.Update, p, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(ItemQueries.SoftDelete, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }
}