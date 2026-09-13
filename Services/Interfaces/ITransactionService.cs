using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface ITransactionService
{
    // =========================================================
    // BUDGET
    // =========================================================

    Task<Budget?> GetBudgetForTransactionAsync(
        Transaction transaction);

    Task<decimal> GetCurrentCategorySpendingAsync(
        Transaction transaction);


    // =========================================================
    // BALANCE
    // =========================================================

    Task<decimal> GetAvailableBalanceAsync(
        int userId);


    // =========================================================
    // TRANSACTIONS
    // =========================================================

    Task<TransactionPageResult> GetTransactionsAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type,
        int currentPage,
        int pageSize);

    Task<IEnumerable<Category>> GetCategoriesAsync();

    Task<IEnumerable<Category>> GetCategoriesByTypeAsync(
        string type);

    Task<Transaction?> GetTransactionAsync(
        int transactionId,
        int userId);

    Task AddTransactionAsync(
        Transaction transaction);

    Task UpdateTransactionAsync(
        Transaction transaction);

    Task DeleteTransactionAsync(
        int transactionId,
        int userId);
}


// =============================================================
// PAGED TRANSACTION RESULT
// =============================================================

public class TransactionPageResult
{
    public IEnumerable<Transaction> Transactions { get; set; }
        = new List<Transaction>();

    public TransactionSummary Summary { get; set; }
        = new();

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}