using System.Data;
using Microsoft.Data.SqlClient;

namespace UniShareProject.Repository.Data.Interfaces;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}