using Dapper;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.Repository.Sql;

namespace UniShareProject.Repository.Implementations;

public class UserRepository : IUserRepository
{
    public UserRepository() {}

    // Auth helper methods now require a unit of work
    public async Task<bool> EmailExistsAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var count = await unitOfWork.Connection.QuerySingleAsync<int>(
            UserQueries.EmailExists,
            new { Email = email },
            unitOfWork.Transaction
        );
        return count > 0;
    }

    public async Task<int> InsertAsync(User u, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        return await unitOfWork.Connection.QuerySingleAsync<int>(UserQueries.Insert, u, unitOfWork.Transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(
            UserQueries.GetByEmail,
            new { Email = email },
            unitOfWork.Transaction
        );
    }

    // Legacy methods for backward compatibility
    public async Task<User?> GetByIdAsync(int id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetById, new { Id = id }, unitOfWork.Transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetByEmail, new { Email = email }, unitOfWork.Transaction);
    }

    public async Task<User?> GetByUsernameAsync(string username, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetByUsername, new { Username = username }, unitOfWork.Transaction);
    }

    public async Task<int> CreateAsync(User user, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QuerySingleAsync<int>(UserQueries.Insert, user, unitOfWork.Transaction);
    }

    public async Task<bool> UpdateAsync(User user, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(UserQueries.Update, user, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork)
    {
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(UserQueries.SoftDelete, new { Id = id }, unitOfWork.Transaction);
        return affectedRows > 0;
    }

    public async Task<IEnumerable<User>> GetAllAsync(IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryAsync<User>(UserQueries.GetAll, transaction: unitOfWork.Transaction);
    }
}