using Dapper;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Tests.Integration;

public abstract class IntegrationTestBase
{
    private readonly string _connectionString;

    protected IntegrationTestBase()
    {
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .Build();

        _connectionString =
            configuration.GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException(
                "TestConnection is not configured.");
    }

    protected TransactionRepository CreateTransactionRepository()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            _connectionString
                    })
                .Build();

        return new TransactionRepository(configuration);
    }

    protected async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    protected async Task<(int UserId, int IncomeCategoryId, int ExpenseCategoryId)>
        CreateTestUserAsync()
    {
        await using var connection = await OpenConnectionAsync();

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var suffix = Guid.NewGuid().ToString("N");

            // ---------------------------------------------------------
            // ENSURE ROLE EXISTS
            // ---------------------------------------------------------

            var roleId =
                await connection.ExecuteScalarAsync<int?>(
                    """
                    SELECT TOP 1 RoleId
                    FROM Roles
                    ORDER BY RoleId;
                    """,
                    transaction: transaction);

            if (!roleId.HasValue)
            {
                roleId =
                    await connection.ExecuteScalarAsync<int>(
                        """
                        INSERT INTO Roles
                        (
                            RoleName
                        )
                        OUTPUT INSERTED.RoleId
                        VALUES
                        (
                            @RoleName
                        );
                        """,
                        new
                        {
                            RoleName = $"TestRole-{suffix[..8]}"
                        },
                        transaction);
            }

            // ---------------------------------------------------------
            // ENSURE INCOME CATEGORY TYPE EXISTS
            // ---------------------------------------------------------

            var incomeTypeId =
                await connection.ExecuteScalarAsync<int?>(
                    """
                    SELECT CategoryTypeId
                    FROM CategoryTypes
                    WHERE TypeName = 'Income';
                    """,
                    transaction: transaction);

            if (!incomeTypeId.HasValue)
            {
                incomeTypeId =
                    await connection.ExecuteScalarAsync<int>(
                        """
                        INSERT INTO CategoryTypes
                        (
                            TypeName
                        )
                        OUTPUT INSERTED.CategoryTypeId
                        VALUES
                        (
                            'Income'
                        );
                        """,
                        transaction: transaction);
            }

            // ---------------------------------------------------------
            // ENSURE EXPENSE CATEGORY TYPE EXISTS
            // ---------------------------------------------------------

            var expenseTypeId =
                await connection.ExecuteScalarAsync<int?>(
                    """
                    SELECT CategoryTypeId
                    FROM CategoryTypes
                    WHERE TypeName = 'Expense';
                    """,
                    transaction: transaction);

            if (!expenseTypeId.HasValue)
            {
                expenseTypeId =
                    await connection.ExecuteScalarAsync<int>(
                        """
                        INSERT INTO CategoryTypes
                        (
                            TypeName
                        )
                        OUTPUT INSERTED.CategoryTypeId
                        VALUES
                        (
                            'Expense'
                        );
                        """,
                        transaction: transaction);
            }

            // ---------------------------------------------------------
            // CREATE TEST USER
            // ---------------------------------------------------------

            var userId =
                await connection.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO Users
                    (
                        FirstName,
                        LastName,
                        Email,
                        PasswordHash,
                        RoleId,
                        IsActive,
                        CreatedOn
                    )
                    OUTPUT INSERTED.UserId
                    VALUES
                    (
                        @FirstName,
                        @LastName,
                        @Email,
                        @PasswordHash,
                        @RoleId,
                        1,
                        SYSUTCDATETIME()
                    );
                    """,
                    new
                    {
                        FirstName = "Integration",
                        LastName = "Test",
                        Email = $"integration-{suffix}@test.local",
                        PasswordHash = "integration-test",
                        RoleId = roleId.Value
                    },
                    transaction);

            // ---------------------------------------------------------
            // CREATE TEST INCOME CATEGORY
            // ---------------------------------------------------------

            var incomeCategoryId =
                await connection.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO Categories
                    (
                        CategoryName,
                        CategoryTypeId,
                        IsActive
                    )
                    OUTPUT INSERTED.CategoryId
                    VALUES
                    (
                        @CategoryName,
                        @CategoryTypeId,
                        1
                    );
                    """,
                    new
                    {
                        CategoryName = $"Test Income {suffix[..8]}",
                        CategoryTypeId = incomeTypeId.Value
                    },
                    transaction);

            // ---------------------------------------------------------
            // CREATE TEST EXPENSE CATEGORY
            // ---------------------------------------------------------

            var expenseCategoryId =
                await connection.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO Categories
                    (
                        CategoryName,
                        CategoryTypeId,
                        IsActive
                    )
                    OUTPUT INSERTED.CategoryId
                    VALUES
                    (
                        @CategoryName,
                        @CategoryTypeId,
                        1
                    );
                    """,
                    new
                    {
                        CategoryName = $"Test Expense {suffix[..8]}",
                        CategoryTypeId = expenseTypeId.Value
                    },
                    transaction);

            await transaction.CommitAsync();

            return (
                userId,
                incomeCategoryId,
                expenseCategoryId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    protected async Task<int> InsertIncomeAsync(
        int userId,
        int categoryId,
        decimal amount)
    {
        await using var connection = await OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO Transactions
            (
                UserId,
                CategoryId,
                Amount,
                TransactionDate,
                Description,
                TransactionType,
                CreatedOn
            )
            OUTPUT INSERTED.TransactionId
            VALUES
            (
                @UserId,
                @CategoryId,
                @Amount,
                CAST(GETDATE() AS date),
                'Integration test income',
                'Income',
                SYSUTCDATETIME()
            );
            """,
            new
            {
                UserId = userId,
                CategoryId = categoryId,
                Amount = amount
            });
    }

    protected async Task<decimal> GetBalanceAsync(int userId)
    {
        await using var connection = await OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT
                COALESCE(
                    SUM(
                        CASE
                            WHEN ct.TypeName = 'Income'
                                THEN t.Amount

                            WHEN ct.TypeName = 'Expense'
                                THEN -t.Amount

                            ELSE 0
                        END
                    ),
                    0
                )
            FROM Transactions t
            INNER JOIN Categories c
                ON c.CategoryId = t.CategoryId
            INNER JOIN CategoryTypes ct
                ON ct.CategoryTypeId = c.CategoryTypeId
            WHERE t.UserId = @UserId;
            """,
            new
            {
                UserId = userId
            });
    }
}