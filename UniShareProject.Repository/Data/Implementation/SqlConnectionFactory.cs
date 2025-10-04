using Microsoft.Data.SqlClient;
using UniShareProject.Repository.Data.Interfaces;

namespace UniShareProject.Repository.Data.Implementation;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}