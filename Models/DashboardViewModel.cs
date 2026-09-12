namespace FinTrack.Models;

public class DashboardViewModel
{
    public TransactionSummary Summary { get; set; } = new();

    public TransactionSummary CurrentMonth { get; set; } = new();

    public TransactionSummary PreviousMonth { get; set; } = new();

    public IEnumerable<Budget> Budgets { get; set; } =
        new List<Budget>();

    public IEnumerable<Transaction> RecentTransactions { get; set; } =
        new List<Transaction>();

    public IEnumerable<DailySpending> LastSevenDays { get; set; } =
        new List<DailySpending>();

    public IEnumerable<MonthlyFinancialPoint> MonthlyTrend { get; set; } =
        new List<MonthlyFinancialPoint>();

    public IEnumerable<UpcomingPayment> UpcomingPayments { get; set; } =
        new List<UpcomingPayment>();

    public decimal SavingsThisMonth =>
        CurrentMonth.TotalIncome -
        CurrentMonth.TotalExpense;

    public decimal SavingsLastMonth =>
        PreviousMonth.TotalIncome -
        PreviousMonth.TotalExpense;

    public decimal SavingsChange
    {
        get
        {
            if (SavingsLastMonth == 0)
                return SavingsThisMonth == 0 ? 0 : 100;

            return
                ((SavingsThisMonth - SavingsLastMonth)
                / Math.Abs(SavingsLastMonth)) * 100;
        }
    }
}

public class DailySpending
{
    public DateTime Date { get; set; }

    public decimal Amount { get; set; }
}

public class MonthlyFinancialPoint
{
    public string Label { get; set; } = "";

    public decimal Income { get; set; }

    public decimal Expense { get; set; }

    public decimal Savings =>
        Income - Expense;
}

public class UpcomingPayment
{
    public string PaymentName { get; set; } = "";

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string Frequency { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public int DaysUntil
    {
        get
        {
            return Math.Max(
                0,
                (PaymentDate.Date - DateTime.Today).Days);
        }
    }
}