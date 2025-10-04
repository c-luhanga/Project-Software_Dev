using System.Data;
using Microsoft.Data.SqlClient;

namespace UniShareProject.Repository.Data.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    SqlConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    Task CommitAsync();
    Task RollbackAsync();
}