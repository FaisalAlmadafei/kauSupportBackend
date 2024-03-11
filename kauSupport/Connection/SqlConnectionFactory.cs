using System.Data;
using System.Data.SqlClient;

namespace kauSupport.Connection;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public IDbConnection CreateConnection()
    {

        return new SqlConnection(_connectionString);
    }
}