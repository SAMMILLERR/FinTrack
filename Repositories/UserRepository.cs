using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConfiguration _configuration;

    // =========================================================
    // DEFAULT STARTING BALANCE
    // =========================================================

    private const decimal StartingBalance = 100000m;

    public UserRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    // =========================================================
    // GET USER BY EMAIL
    // =========================================================

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

    // =========================================================
    // ADD USER
    // =========================================================

    public async Task AddAsync(User user)
    {
        using var connection = GetConnection();

        await connection.OpenAsync();

        using var transaction =
            await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            // =====================================================
            // CREATE USER
            // =====================================================

            const string userSql = @"
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
                );

                SELECT CAST(
                    SCOPE_IDENTITY()
                    AS INT
                );";

            var userId =
                await connection.ExecuteScalarAsync<int>(
                    userSql,
                    user,
                    transaction);


            // =====================================================
            // FIND ACTIVE INCOME CATEGORY
            // =====================================================

            const string incomeCategorySql = @"
                SELECT TOP 1
                    c.CategoryId
                FROM Categories c
                INNER JOIN CategoryTypes ct
                    ON ct.CategoryTypeId =
                       c.CategoryTypeId
                WHERE ct.TypeName = 'Income'
                  AND c.IsActive = 1
                ORDER BY c.CategoryId;";

            var incomeCategoryId =
                await connection.QueryFirstOrDefaultAsync<int>(
                    incomeCategorySql,
                    transaction: transaction);


            if (incomeCategoryId <= 0)
            {
                throw new InvalidOperationException(
                    "No active income category is configured.");
            }


            // =====================================================
            // CREATE OPENING BALANCE
            // =====================================================

            const string openingBalanceSql = @"
                INSERT INTO Transactions
                (
                    UserId,
                    CategoryId,
                    Amount,
                    TransactionDate,
                    Description,
                    TransactionType,
                    RecurringPaymentId
                )
                VALUES
                (
                    @UserId,
                    @CategoryId,
                    @Amount,
                    @TransactionDate,
                    @Description,
                    'Income',
                    NULL
                );";

            await connection.ExecuteAsync(
                openingBalanceSql,
                new
                {
                    UserId = userId,

                    CategoryId =
                        incomeCategoryId,

                    Amount =
                        StartingBalance,

                    TransactionDate =
                        DateTime.UtcNow,

                    Description =
                        "Opening balance"
                },
                transaction);


            // =====================================================
            // COMMIT USER + OPENING BALANCE
            // =====================================================

            await transaction.CommitAsync();
        }
        catch
        {
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Preserve the original exception.
                }
            }

            throw;
        }
    }

    // =========================================================
    // GET ALL USERS
    // =========================================================

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

    // =========================================================
    // UPDATE ROLE
    // =========================================================

    public async Task UpdateRoleAsync(
        int userId,
        int roleId)
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

    // =========================================================
    // GET USER BY ID
    // =========================================================

    public async Task<User?> GetByIdAsync(
        int userId)
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

    // =========================================================
    // UPDATE STATUS
    // =========================================================

    public async Task UpdateStatusAsync(
        int userId,
        bool isActive)
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