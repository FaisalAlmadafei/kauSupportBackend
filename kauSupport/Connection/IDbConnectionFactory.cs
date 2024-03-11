using System.Data;

namespace kauSupport.Connection;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
