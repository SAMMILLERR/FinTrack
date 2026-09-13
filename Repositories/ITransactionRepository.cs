using FinTrack.Models;

namespace FinTrack.Repositories;

public interface ITransactionRepository
{
    // =========================================================
    // BASIC TRANSACTION OPERATIONS
    // =========================================================

    Task<IEnumerable<Transaction>> GetByUserAsync(
        int userId);

    Task<Transaction?> GetByIdAsync(
        int transactionId,
        int userId);

    Task AddAsync(
        Transaction transaction);

    Task UpdateAsync(
        Transaction transaction);

    Task DeleteAsync(
        int transactionId,
        int userId);


    // =========================================================
    // ADMIN / GENERAL TRANSACTION OPERATIONS
    // =========================================================

    Task<IEnumerable<Transaction>> GetAllAsync();

    Task<IEnumerable<Transaction>> SearchAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type);


    // =========================================================
    // PAGED SEARCH
    // =========================================================

    Task<IEnumerable<Transaction>> SearchPagedAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type,
        int currentPage,
        int pageSize);

    Task<int> GetSearchCountAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type);


    // =========================================================
    // SUMMARY
    // =========================================================

    Task<TransactionSummary> GetSummaryAsync(
        int userId);


    // =========================================================
    // AVAILABLE BALANCE
    // =========================================================

    Task<decimal> GetAvailableBalanceAsync(
        int userId);
}