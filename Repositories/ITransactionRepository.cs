using FinTrack.Models;

namespace FinTrack.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserAsync(int userId);

    Task<Transaction?> GetByIdAsync(int transactionId, int userId);

    Task AddAsync(Transaction transaction);

    Task UpdateAsync(Transaction transaction);

    Task DeleteAsync(int transactionId, int userId);

    Task<TransactionSummary> GetSummaryAsync(int userId);
    Task<IEnumerable<Transaction>> GetAllAsync();

    Task<IEnumerable<Transaction>> SearchAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type);
    Task<IEnumerable<Transaction>> SearchPagedAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type,
    int page,
    int pageSize);

    Task<int> GetSearchCountAsync(
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int? categoryId,
    string? type);
}
