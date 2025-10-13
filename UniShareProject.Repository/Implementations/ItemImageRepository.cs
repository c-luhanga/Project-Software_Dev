using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;

namespace UniShareProject.Repository.Implementations;

/// <summary>
/// Implementation of item image repository using Dapper
/// </summary>
public class ItemImageRepository : IItemImageRepository
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ItemImageRepository(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<int> InsertAsync(int itemId, string url, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            var affectedRows = await unitOfWork.Connection.QuerySingleAsync<int>(
                ItemImageQueries.Insert, 
                new { ItemId = itemId, Url = url }, 
                unitOfWork.Transaction);
            
            await unitOfWork.CommitAsync();
            return affectedRows;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<int> CountByItemAsync(int itemId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        return await unitOfWork.Connection.QuerySingleAsync<int>(
            ItemImageQueries.CountByItem, 
            new { ItemId = itemId }, 
            unitOfWork.Transaction);
    }

    public async Task<IReadOnlyList<string>> GetUrlsAsync(int itemId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        var urls = await unitOfWork.Connection.QueryAsync<string>(
            ItemImageQueries.GetUrlsByItem, 
            new { ItemId = itemId }, 
            unitOfWork.Transaction);
        
        return urls.ToList().AsReadOnly();
    }
}