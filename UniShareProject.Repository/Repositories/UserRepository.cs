using Dapper;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public class UserRepository : IUserRepository
{
    public UserRepository() {}

    // Auth helper methods now require a unit of work
    public async Task<bool> EmailExistsAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email";
        var count = await unitOfWork.Connection.QuerySingleAsync<int>(
            sql,
            new { Email = email },
            unitOfWork.Transaction
        );
        return count > 0;
    }

    public async Task<int> InsertAsync(User u, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        const string sql = @"
                INSERT INTO dbo.Users 
                (FirebaseUID, FirstName, LastName, Email, PasswordHash, Phone, House, IsBanned, IsAdmin, IsDeleted, CreatedAt, LastSeen, ProfileImageURL)
                VALUES 
                (@FirebaseUID, @FirstName, @LastName, @Email, @PasswordHash, @Phone, @House, @IsBanned, @IsAdmin, @IsDeleted, @CreatedAt, @LastSeen, @ProfileImageURL);
                SELECT CAST(SCOPE_IDENTITY() as int)";
        return await unitOfWork.Connection.QuerySingleAsync<int>(sql, u, unitOfWork.Transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct)
    {
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