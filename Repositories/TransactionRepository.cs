using Dapper;
using FinTrack.Models;
using Microsoft.Data.SqlClient;

namespace FinTrack.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IConfiguration _configuration;

    public TransactionRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection GetConnection()
    {
        return new SqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }

    public async Task<IEnumerable<Transaction>> GetByUserAsync(int userId)
    {
        using var connection = GetConnection();

        string sql = @"
        SELECT
    t.TransactionId,
    t.UserId,
    t.CategoryId,
    c.CategoryName,
    ct.TypeName AS TransactionType,
    t.Amount,
    t.TransactionDate,
    t.Description,
    t.CreatedOn
FROM Transactions t
INNER JOIN Categories c
    ON t.CategoryId = c.CategoryId
INNER JOIN CategoryTypes ct
    ON c.CategoryTypeId = ct.CategoryTypeId
WHERE t.UserId = @userId
ORDER BY t.TransactionDate DESC;";

        return await connection.QueryAsync<Transaction>(
            sql,
            new { userId });
    }

 public async Task<Transaction?> GetByIdAsync(
    int transactionId,
    int userId)
{
    using var connection = GetConnection();

    string sql = @"
    SELECT
        t.TransactionId,
        t.UserId,
        t.CategoryId,
        c.CategoryName,
        ct.TypeName AS TransactionType,
        t.Amount,
        t.TransactionDate,
        t.Description,
        t.CreatedOn
    FROM Transactions t

    INNER JOIN Categories c
        ON t.CategoryId = c.CategoryId

    INNER JOIN CategoryTypes ct
        ON c.CategoryTypeId = ct.CategoryTypeId

    WHERE
        t.TransactionId = @transactionId
        AND
        t.UserId = @userId";

    return await connection.QueryFirstOrDefaultAsync<Transaction>(
        sql,
        new
        {
            transactionId,
            userId
        });
}

    public async Task AddAsync(Transaction transaction)
    {
        using var connection = GetConnection();

        string sql = @"
        INSERT INTO Transactions
        (
            UserId,
            CategoryId,
            Amount,
            TransactionDate,
            Description
        )
        VALUES
        (
            @UserId,
            @CategoryId,
            @Amount,
            @TransactionDate,
            @Description
        )";

        await connection.ExecuteAsync(sql, transaction);
    }
public async Task<int> GetSearchCountAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type)
{
    using var connection = GetConnection();

    string sql = @"

SELECT COUNT(*)

FROM Transactions t

INNER JOIN Categories c
ON t.CategoryId = c.CategoryId

INNER JOIN CategoryTypes ct
ON c.CategoryTypeId = ct.CategoryTypeId

WHERE

(@UserId IS NULL OR t.UserId=@UserId)

AND

(@FromDate IS NULL OR t.TransactionDate>=@FromDate)

AND

(@ToDate IS NULL OR t.TransactionDate<=@ToDate)

AND

(@CategoryId IS NULL OR t.CategoryId=@CategoryId)

AND

(@Type IS NULL OR ct.TypeName=@Type)";

    return await connection.ExecuteScalarAsync<int>(
        sql,
        new
        {
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            CategoryId = categoryId,
            Type = type
        });
}
public async Task<IEnumerable<Transaction>> SearchPagedAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type,
    int page,
    int pageSize)
{
    using var connection = GetConnection();

    int skip = (page - 1) * pageSize;

    string sql = @"

SELECT

t.TransactionId,
t.UserId,
u.FirstName + ' ' + u.LastName AS UserName,
t.CategoryId,
c.CategoryName,
ct.TypeName AS TransactionType,
t.Amount,
t.TransactionDate,
t.Description,
t.CreatedOn

FROM Transactions t

INNER JOIN Users u
ON t.UserId=u.UserId

INNER JOIN Categories c
ON t.CategoryId=c.CategoryId

INNER JOIN CategoryTypes ct
ON c.CategoryTypeId=ct.CategoryTypeId

WHERE

(@UserId IS NULL OR t.UserId=@UserId)

AND

(@FromDate IS NULL OR t.TransactionDate>=@FromDate)

AND

(@ToDate IS NULL OR t.TransactionDate<=@ToDate)

AND

(@CategoryId IS NULL OR t.CategoryId=@CategoryId)

AND

(@Type IS NULL OR ct.TypeName=@Type)

ORDER BY

t.TransactionDate DESC,
t.TransactionId DESC

OFFSET @Skip ROWS
FETCH NEXT @PageSize ROWS ONLY;";

    return await connection.QueryAsync<Transaction>(
        sql,
        new
        {
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            CategoryId = categoryId,
            Type = type,
            Skip = skip,
            PageSize = pageSize
        });
}
 public async Task UpdateAsync(Transaction transaction)
{
    using var connection = GetConnection();

    string sql = @"
    UPDATE Transactions

    SET
        CategoryId = @CategoryId,
        Amount = @Amount,
        TransactionDate = @TransactionDate,
        Description = @Description

    WHERE
        TransactionId = @TransactionId
        AND
        UserId = @UserId";

    await connection.ExecuteAsync(sql, transaction);
}
public async Task<TransactionSummary> GetSummaryAsync(int userId)
{
    using var connection = GetConnection();

    string sql = @"

SELECT

SUM(CASE
WHEN ct.TypeName='Income'
THEN t.Amount
ELSE 0
END) AS TotalIncome,

SUM(CASE
WHEN ct.TypeName='Expense'
THEN t.Amount
ELSE 0
END) AS TotalExpense

FROM Transactions t

INNER JOIN Categories c

ON t.CategoryId=c.CategoryId

INNER JOIN CategoryTypes ct

ON c.CategoryTypeId=ct.CategoryTypeId

WHERE t.UserId=@userId";

    var summary =
        await connection.QueryFirstOrDefaultAsync<TransactionSummary>(
            sql,
            new { userId });

    return summary ?? new TransactionSummary();
}
public async Task<IEnumerable<Transaction>> GetAllAsync()
{
    using var connection = GetConnection();

    string sql = @"
    SELECT
        t.TransactionId,
        t.UserId,
        u.FirstName + ' ' + u.LastName AS UserName,
        t.CategoryId,
        c.CategoryName,
        ct.TypeName AS TransactionType,
        t.Amount,
        t.TransactionDate,
        t.Description,
        t.CreatedOn

    FROM Transactions t

    INNER JOIN Users u
        ON t.UserId = u.UserId

    INNER JOIN Categories c
        ON t.CategoryId = c.CategoryId

    INNER JOIN CategoryTypes ct
        ON c.CategoryTypeId = ct.CategoryTypeId

    ORDER BY
        t.TransactionDate DESC,
        t.TransactionId DESC;";

    return await connection.QueryAsync<Transaction>(sql);
}
public async Task<IEnumerable<Transaction>> SearchAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type)
{
    using var connection = GetConnection();

    string sql = @"

SELECT

t.TransactionId,
t.UserId,
u.FirstName + ' ' + u.LastName AS UserName,
t.CategoryId,
c.CategoryName,
ct.TypeName AS TransactionType,
t.Amount,
t.TransactionDate,
t.Description,
t.CreatedOn

FROM Transactions t

INNER JOIN Users u
ON t.UserId=u.UserId

INNER JOIN Categories c
ON t.CategoryId=c.CategoryId

INNER JOIN CategoryTypes ct
ON c.CategoryTypeId=ct.CategoryTypeId

WHERE

(@UserId IS NULL OR t.UserId=@UserId)

AND

(@FromDate IS NULL OR t.TransactionDate>=@FromDate)

AND

(@ToDate IS NULL OR t.TransactionDate<=@ToDate)

AND

(@CategoryId IS NULL OR t.CategoryId=@CategoryId)

AND

(@Type IS NULL OR ct.TypeName=@Type)

ORDER BY
t.TransactionDate DESC,
t.TransactionId DESC";

    return await connection.QueryAsync<Transaction>(
        sql,
        new
        {
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            CategoryId = categoryId,
            Type = type
        });
}
public async Task DeleteAsync(
    int transactionId,
    int userId)
{
    using var connection = GetConnection();

    string sql = @"
    DELETE FROM Transactions

    WHERE
        TransactionId = @transactionId
        AND
        UserId = @userId";

    await connection.ExecuteAsync(
        sql,
        new
        {
            transactionId,
            userId
        });
}
}