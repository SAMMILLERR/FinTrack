using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IRecurringPaymentRepository _recurringPaymentRepository;

    public DashboardService(
        ITransactionRepository transactionRepository,
        IBudgetRepository budgetRepository,
        IRecurringPaymentRepository recurringPaymentRepository)
    {
        _transactionRepository = transactionRepository;
        _budgetRepository = budgetRepository;
        _recurringPaymentRepository = recurringPaymentRepository;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int userId)
    {
        var dashboard = new DashboardViewModel();

        var transactions =
            (await _transactionRepository.GetByUserAsync(userId))
            .ToList();

        var today = DateTime.Today;

        var currentMonthStart =
            new DateTime(today.Year, today.Month, 1);

        var previousMonthStart =
            currentMonthStart.AddMonths(-1);

        var nextMonthStart =
            currentMonthStart.AddMonths(1);

        // Overall summary
        dashboard.Summary =
            await _transactionRepository.GetSummaryAsync(userId);

        // Current month
        var currentMonthTransactions =
            transactions
                .Where(t =>
                    t.TransactionDate.Date >= currentMonthStart &&
                    t.TransactionDate.Date < nextMonthStart)
                .ToList();

        dashboard.CurrentMonth =
            BuildSummary(currentMonthTransactions);

        // Previous month
        var previousMonthTransactions =
            transactions
                .Where(t =>
                    t.TransactionDate.Date >= previousMonthStart &&
                    t.TransactionDate.Date < currentMonthStart)
                .ToList();

        dashboard.PreviousMonth =
            BuildSummary(previousMonthTransactions);

        // Current month budgets
        dashboard.Budgets =
            (await _budgetRepository.GetByUserAsync(userId))
            .Where(b =>
                b.BudgetMonth == today.Month &&
                b.BudgetYear == today.Year)
            .OrderByDescending(b => b.UsagePercentage)
            .ToList();

        // Recent transactions
        dashboard.RecentTransactions =
            transactions
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.TransactionId)
                .Take(6)
                .ToList();

        // Last seven days
        var sevenDaysAgo = today.AddDays(-6);

        dashboard.LastSevenDays =
            Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = sevenDaysAgo.AddDays(offset);

                    return new DailySpending
                    {
                        Date = date,

                        Amount =
                            transactions
                                .Where(t =>
                                    t.TransactionDate.Date == date &&
                                    t.TransactionType.Equals(
                                        "Expense",
                                        StringComparison.OrdinalIgnoreCase))
                                .Sum(t => t.Amount)
                    };
                })
                .ToList();

        // Six-month trend
        dashboard.MonthlyTrend =
            Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var month =
                        today.AddMonths(-5 + index);

                    var monthStart =
                        new DateTime(
                            month.Year,
                            month.Month,
                            1);

                    var monthEnd =
                        monthStart.AddMonths(1);

                    var monthTransactions =
                        transactions
                            .Where(t =>
                                t.TransactionDate.Date >= monthStart &&
                                t.TransactionDate.Date < monthEnd)
                            .ToList();

                    return new MonthlyFinancialPoint
                    {
                        Label = monthStart.ToString("MMM"),

                        Income =
                            monthTransactions
                                .Where(t =>
                                    t.TransactionType.Equals(
                                        "Income",
                                        StringComparison.OrdinalIgnoreCase))
                                .Sum(t => t.Amount),

                        Expense =
                            monthTransactions
                                .Where(t =>
                                    t.TransactionType.Equals(
                                        "Expense",
                                        StringComparison.OrdinalIgnoreCase))
                                .Sum(t => t.Amount)
                    };
                })
                .ToList();

        // Upcoming recurring payments
        var recurringPayments =
            await _recurringPaymentRepository
                .GetByUserIdAsync(userId);

        dashboard.UpcomingPayments =
            recurringPayments
                .Where(p =>
                    p.IsActive &&
                    p.NextPaymentDate.Date >= today &&
                    p.NextPaymentDate.Date <= today.AddDays(7))
                .OrderBy(p => p.NextPaymentDate)
                .Take(5)
                .Select(p => new UpcomingPayment
                {
                    PaymentName = p.PaymentName,
                    Amount = p.Amount,
                    PaymentDate = p.NextPaymentDate,
                    Frequency = p.Frequency,
                    CategoryName = p.CategoryName
                })
                .ToList();

        return dashboard;
    }

    private static TransactionSummary BuildSummary(
        IEnumerable<Transaction> transactions)
    {
        var income =
            transactions
                .Where(t =>
                    t.TransactionType.Equals(
                        "Income",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

        var expense =
            transactions
                .Where(t =>
                    t.TransactionType.Equals(
                        "Expense",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

        // Balance is calculated automatically by TransactionSummary.
        return new TransactionSummary
        {
            TotalIncome = income,
            TotalExpense = expense
        };
    }
}