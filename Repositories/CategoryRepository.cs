using Dapper;
using Microsoft.Data.SqlClient;
using FinTrack.Models;

namespace FinTrack.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IConfiguration _configuration;

    public CategoryRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var connection = GetConnection();

        string sql = @"
            SELECT
                c.CategoryId,
                c.CategoryName,
                c.CategoryTypeId,
                ct.TypeName AS CategoryTypeName,
                c.IsActive
            FROM Categories c
            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId
            ORDER BY c.CategoryId";

        return await connection.QueryAsync<Category>(sql);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var connection = GetConnection();

        string sql = @"
            SELECT
                c.CategoryId,
                c.CategoryName,
                c.CategoryTypeId,
                ct.TypeName AS CategoryTypeName,
                c.IsActive
            FROM Categories c
            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId
            WHERE c.CategoryId = @id";

        return await connection.QueryFirstOrDefaultAsync<Category>(
            sql,
            new { id });
    }

    public async Task AddAsync(Category category)
    {
        using var connection = GetConnection();

        string sql = @"
            INSERT INTO Categories
            (
                CategoryName,
                CategoryTypeId,
                IsActive
            )
            VALUES
            (
                @CategoryName,
                @CategoryTypeId,
                @IsActive
            )";

        await connection.ExecuteAsync(sql, category);
    }

    public async Task UpdateAsync(Category category)
    {
        using var connection = GetConnection();

        string sql = @"
            UPDATE Categories
            SET
                CategoryName = @CategoryName,
                CategoryTypeId = @CategoryTypeId,
                IsActive = @IsActive
            WHERE CategoryId = @CategoryId";

        await connection.ExecuteAsync(sql, category);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = GetConnection();

        await connection.ExecuteAsync(
            "DELETE FROM Categories WHERE CategoryId = @id",
            new { id });
    }

    public async Task ToggleStatusAsync(int id)
    {
        using var connection = GetConnection();

        string sql = @"
            UPDATE Categories
            SET IsActive =
                CASE
                    WHEN IsActive = 1 THEN 0
                    ELSE 1
                END
            WHERE CategoryId = @id";

        await connection.ExecuteAsync(
            sql,
            new { id });
    }

    public async Task<IEnumerable<CategoryType>> GetCategoryTypesAsync()
    {
        using var connection = GetConnection();

        return await connection.QueryAsync<CategoryType>(
            @"SELECT *
              FROM CategoryTypes
              ORDER BY TypeName");
    }

    public async Task<IEnumerable<Category>> GetByTypeAsync(string type)
    {
        using var connection = GetConnection();

        string sql = @"
            SELECT
                c.CategoryId,
                c.CategoryName,
                c.CategoryTypeId,
                ct.TypeName AS CategoryTypeName,
                c.IsActive
            FROM Categories c
            INNER JOIN CategoryTypes ct
                ON c.CategoryTypeId = ct.CategoryTypeId
            WHERE ct.TypeName = @type
              AND c.IsActive = 1
            ORDER BY c.CategoryName";

        return await connection.QueryAsync<Category>(
            sql,
            new { type });
    }
}