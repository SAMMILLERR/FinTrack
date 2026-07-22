using Microsoft.Data.SqlClient;

namespace FinTrack.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}