using System.Data;
using Microsoft.Data.SqlClient;
using UniShareProject.Repository.Data.Interfaces;

namespace UniShareProject.Repository.Data.Implementation;

public class UnitOfWork : IUnitOfWork
{
    public SqlConnection Connection { get; }
    public IDbTransaction? Transaction { get; private set; }

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        Connection = connectionFactory.CreateConnection();
        Connection.Open();
        Transaction = Connection.BeginTransaction();
    }

    public async Task CommitAsync()
    {
        if (Transaction != null)
        {
            await Task.Run(() => Transaction.Commit());
            Transaction.Dispose();
            Transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (Transaction != null)
        {
            await Task.Run(() => Transaction.Rollback());
            Transaction.Dispose();
            Transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction != null)
        {
            Transaction.Dispose();
            Transaction = null;
        }

        if (Connection != null)
        {
            await Connection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}