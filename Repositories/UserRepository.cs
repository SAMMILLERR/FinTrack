using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConfiguration _configuration;

    public UserRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = GetConnection();

        string sql = @"
        SELECT
            UserId,
            FirstName,
            LastName,
            Email,
            PasswordHash,
            RoleId,
            IsActive,
            CreatedOn
        FROM Users
        WHERE Email = @email";

        return await connection.QueryFirstOrDefaultAsync<User>(
            sql,
            new { email });
    }

    public async Task AddAsync(User user)
    {
        using var connection = GetConnection();

        string sql = @"
        INSERT INTO Users
        (
            FirstName,
            LastName,
            Email,
            PasswordHash,
            RoleId,
            IsActive
        )
        VALUES
        (
            @FirstName,
            @LastName,
            @Email,
            @PasswordHash,
            @RoleId,
            @IsActive
        )";

        await connection.ExecuteAsync(sql, user);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var connection = GetConnection();

        string sql = @"
        SELECT
            u.UserId,
            u.FirstName,
            u.LastName,
            u.Email,
            u.RoleId,
            r.RoleName,
            u.IsActive,
            u.CreatedOn
        FROM Users u
        INNER JOIN Roles r
            ON u.RoleId = r.RoleId
        ORDER BY u.FirstName, u.LastName;";

        return await connection.QueryAsync<User>(sql);
    }

    public async Task UpdateRoleAsync(int userId, int roleId)
    {
        using var connection = GetConnection();

        string sql = @"
        UPDATE Users
        SET RoleId = @roleId
        WHERE UserId = @userId";

        await connection.ExecuteAsync(
            sql,
            new
            {
                userId,
                roleId
            });
    }
public async Task<User?> GetByIdAsync(int userId)
{
    using var connection = GetConnection();

    string sql = @"
        SELECT
            UserId,
            FirstName,
            LastName,
            Email,
            PasswordHash,
            RoleId,
            IsActive,
            CreatedOn
        FROM Users
        WHERE UserId = @userId";

    return await connection.QueryFirstOrDefaultAsync<User>(
        sql,
        new { userId });
}
    public async Task UpdateStatusAsync(int userId, bool isActive)
    {
        using var connection = GetConnection();

        string sql = @"
        UPDATE Users
        SET IsActive = @isActive
        WHERE UserId = @userId";

        await connection.ExecuteAsync(
            sql,
            new
            {
                userId,
                isActive
            });
    }
}