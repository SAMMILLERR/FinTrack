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


    // =========================================================
    // CATEGORIES
    // =========================================================

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }


    public async Task<IEnumerable<Category>> GetCategoriesByTypeAsync(
        string type)
    {
        return await _categoryRepository.GetByTypeAsync(type);
    }


    // =========================================================
    // GET TRANSACTION
    // =========================================================

    public async Task<Transaction?> GetTransactionAsync(
        int transactionId,
        int userId)
    {
        return await _transactionRepository.GetByIdAsync(
            transactionId,
            userId);
    }


    // =========================================================
    // ADD
    // =========================================================

    public async Task AddTransactionAsync(
        Transaction transaction)
    {
        if (transaction.UserId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid user.");
        }

        if (transaction.CategoryId <= 0)
        {
            throw new InvalidOperationException(
                "Please select a valid category.");
        }

        if (transaction.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Transaction amount must be greater than zero.");
        }

        await _transactionRepository.AddAsync(
            transaction);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task UpdateTransactionAsync(
        Transaction transaction)
    {
        if (transaction.UserId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid user.");
        }

        if (transaction.TransactionId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid transaction.");
        }

        if (transaction.CategoryId <= 0)
        {
            throw new InvalidOperationException(
                "Please select a valid category.");
        }

        if (transaction.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Transaction amount must be greater than zero.");
        }

        await _transactionRepository.UpdateAsync(
            transaction);
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task DeleteTransactionAsync(
        int transactionId,
        int userId)
    {
        if (transactionId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid transaction.");
        }

        if (userId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid user.");
        }

        await _transactionRepository.DeleteAsync(
            transactionId,
            userId);
    }


    // =========================================================
    // BUDGET
    // =========================================================

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
            await GetBudgetForTransactionAsync(
                transaction);

        return budget?.SpentAmount ?? 0m;
    }


    // =========================================================
    // AVAILABLE BALANCE
    // =========================================================

    public async Task<decimal> GetAvailableBalanceAsync(
        int userId)
    {
        if (userId <= 0)
        {
            throw new InvalidOperationException(
                "Invalid user.");
        }

        return await _transactionRepository
            .GetAvailableBalanceAsync(userId);
    }


    // =========================================================
    // PAGED TRANSACTIONS
    // =========================================================

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

        if (pageSize <= 0)
        {
            pageSize = 20;
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

        if (totalPages > 0 &&
            currentPage > totalPages)
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
                    .GetSummaryAsync(
                        userId.Value);
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