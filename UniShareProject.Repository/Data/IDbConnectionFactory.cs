using System.Data;
using Microsoft.Data.SqlClient;

namespace UniShareProject.Repository.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}