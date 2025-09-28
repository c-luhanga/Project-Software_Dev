using Dapper;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public class ItemRepository : IItemRepository
{
    public async Task<Item?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Items WHERE ItemID = @Id";
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<Item>(sql, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Items WHERE StatusID = 1"; // StatusID 1 = available
        return await unitOfWork.Connection.QueryAsync<Item>(sql, transaction: unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Items WHERE SellerID = @UserId";
        return await unitOfWork.Connection.QueryAsync<Item>(sql, new { UserId = userId }, unitOfWork.Transaction);
    }

    public async Task<IEnumerable<Item>> SearchAsync(string query, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            SELECT i.*, c.CategoryName as Category 
            FROM dbo.Items i
            LEFT JOIN dbo.Categories c ON i.CategoryID = c.CategoryID
            WHERE i.StatusID = 1 AND (i.Title LIKE @Query OR i.Description LIKE @Query OR c.CategoryName LIKE @Query)";
        return await unitOfWork.Connection.QueryAsync<Item>(sql, new { Query = $"%{query}%" }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(Item item, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            INSERT INTO dbo.Items (Title, Description, CategoryID, Price, ConditionID, StatusID, SellerID, PostedDate)
            VALUES (@Title, @Description, @CategoryID, @Price, @ConditionID, @StatusID, @SellerID, @PostedDate);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        return await unitOfWork.Connection.QuerySingleAsync<int>(sql, item, unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(Item item, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            UPDATE dbo.Items 
            SET Title = @Title, Description = @Description, CategoryID = @CategoryID, Price = @Price, 
                ConditionID = @ConditionID, StatusID = @StatusID
            WHERE ItemID = @ItemID";
        
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, item, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = "UPDATE dbo.Items SET StatusID = 0 WHERE ItemID = @Id"; // StatusID 0 = deleted/inactive
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }
}