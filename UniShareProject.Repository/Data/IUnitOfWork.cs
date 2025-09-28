using System.Data;
using Microsoft.Data.SqlClient;

namespace UniShareProject.Repository.Data;

public interface IUnitOfWork : IAsyncDisposable
{
    SqlConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    Task CommitAsync();
    Task RollbackAsync();
}