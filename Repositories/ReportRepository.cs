using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinTrack.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IConfiguration _configuration;

    public ReportRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    public async Task<IEnumerable<CategoryReport>> GetCategoryReportAsync(
        int userId,
        int month,
        int year)
    {
        using var connection = GetConnection();

        return await connection.QueryAsync<CategoryReport>(
            "sp_GetCategoryWiseReport",
            new
            {
                UserId = userId,
                Month = month,
                Year = year
            },
            commandType: CommandType.StoredProcedure);
    }
}