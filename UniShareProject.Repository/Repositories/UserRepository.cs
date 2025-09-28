using Dapper;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // New auth helper methods
    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        await using var unitOfWork = new UnitOfWork(_connectionFactory);
        
        const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email";
        var count = await unitOfWork.Connection.QuerySingleAsync<int>(
            sql, 
            new { Email = email }, 
            unitOfWork.Transaction
        );
        
        return count > 0;
    }

    public async Task<int> InsertAsync(User u, CancellationToken ct)
    {
        await using var unitOfWork = new UnitOfWork(_connectionFactory);
        
        try
        {
            const string sql = @"
                INSERT INTO dbo.Users 
                (FirebaseUID, FirstName, LastName, Email, PasswordHash, Phone, House, IsBanned, IsAdmin, IsDeleted, CreatedAt, LastSeen, ProfileImageURL)
                VALUES 
                (@FirebaseUID, @FirstName, @LastName, @Email, @PasswordHash, @Phone, @House, @IsBanned, @IsAdmin, @IsDeleted, @CreatedAt, @LastSeen, @ProfileImageURL);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            var userId = await unitOfWork.Connection.QuerySingleAsync<int>(sql, u, unitOfWork.Transaction);
            await unitOfWork.CommitAsync();
            
            return userId;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        await using var unitOfWork = new UnitOfWork(_connectionFactory);
        
        const string sql = "SELECT * FROM dbo.Users WHERE Email = @Email AND IsDeleted = 0";
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(
            sql, 
            new { Email = email }, 
            unitOfWork.Transaction
        );
    }

    // Legacy methods for backward compatibility
    public async Task<User?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE UserID = @Id AND IsDeleted = 0";
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE Email = @Email AND IsDeleted = 0";
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, unitOfWork.Transaction);
    }

    public async Task<User?> GetByUsernameAsync(string username, IUnitOfWork unitOfWork)
    {
        // Treat username as email for backward compatibility
        const string sql = "SELECT * FROM dbo.Users WHERE Email = @Username AND IsDeleted = 0";
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(User user, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            INSERT INTO dbo.Users 
            (FirebaseUID, FirstName, LastName, Email, PasswordHash, Phone, House, IsBanned, IsAdmin, IsDeleted, CreatedAt, LastSeen, ProfileImageURL)
            VALUES 
            (@FirebaseUID, @FirstName, @LastName, @Email, @PasswordHash, @Phone, @House, @IsBanned, @IsAdmin, @IsDeleted, @CreatedAt, @LastSeen, @ProfileImageURL);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        return await unitOfWork.Connection.QuerySingleAsync<int>(sql, user, unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(User user, IUnitOfWork unitOfWork)
    {
        const string sql = @"
            UPDATE dbo.Users 
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email, PasswordHash = @PasswordHash, 
                Phone = @Phone, House = @House, IsBanned = @IsBanned, IsAdmin = @IsAdmin, IsDeleted = @IsDeleted,
                LastSeen = @LastSeen, ProfileImageURL = @ProfileImageURL
            WHERE UserID = @UserID";
        
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, user, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        const string sql = "UPDATE dbo.Users SET IsDeleted = 1 WHERE UserID = @Id";
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(sql, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<IEnumerable<User>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE IsDeleted = 0";
        return await unitOfWork.Connection.QueryAsync<User>(sql, transaction: unitOfWork.Transaction);
    }
}