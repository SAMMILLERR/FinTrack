using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBudgetRepository _budgetRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _budgetRepository = budgetRepository;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Category>> GetCategoriesByTypeAsync(
        string type)
    {
        return await _categoryRepository.GetByTypeAsync(type);
    }

    public async Task<Transaction?> GetTransactionAsync(
        int transactionId,
        int userId)
    {
        return await _transactionRepository.GetByIdAsync(
            transactionId,
            userId);
    }

    public async Task AddTransactionAsync(
        Transaction transaction)
    {
        await _transactionRepository.AddAsync(transaction);
    }

    public async Task UpdateTransactionAsync(
        Transaction transaction)
    {
        await _transactionRepository.UpdateAsync(transaction);
    }

    public async Task DeleteTransactionAsync(
        int transactionId,
        int userId)
    {
        await _transactionRepository.DeleteAsync(
            transactionId,
            userId);
    }

    public async Task<Budget?> GetBudgetForTransactionAsync(
        Transaction transaction)
    {
        if (!transaction.TransactionType.Equals(
                "Expense",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var budgets =
            await _budgetRepository.GetByUserAsync(
                transaction.UserId);

        return budgets.FirstOrDefault(b =>
            b.CategoryId == transaction.CategoryId &&
            b.BudgetMonth == transaction.TransactionDate.Month &&
            b.BudgetYear == transaction.TransactionDate.Year);
    }

    public async Task<decimal> GetCurrentCategorySpendingAsync(
        Transaction transaction)
    {
        var budget =
            await GetBudgetForTransactionAsync(transaction);

        return budget?.SpentAmount ?? 0;
    }

    public async Task<TransactionPageResult> GetTransactionsAsync(
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? categoryId,
        string? type,
        int currentPage,
        int pageSize)
    {
        if (currentPage < 1)
        {
            currentPage = 1;
        }

        var totalRecords =
            await _transactionRepository.GetSearchCountAsync(
                userId,
                fromDate,
                toDate,
                categoryId,
                type);

        var totalPages =
            (int)Math.Ceiling(
                totalRecords / (double)pageSize);

        if (totalPages > 0 && currentPage > totalPages)
        {
            currentPage = totalPages;
        }

        var transactions =
            await _transactionRepository.SearchPagedAsync(
                userId,
                fromDate,
                toDate,
                categoryId,
                type,
                currentPage,
                pageSize);

        TransactionSummary summary = new();

        if (userId.HasValue)
        {
            summary =
                await _transactionRepository
                    .GetSummaryAsync(userId.Value);
        }

        return new TransactionPageResult
        {
            Transactions = transactions,
            Summary = summary,
            CurrentPage = currentPage,
            TotalPages = totalPages
        };
    }
}