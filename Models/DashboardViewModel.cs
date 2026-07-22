namespace FinTrack.Models;

public class DashboardViewModel
{
    public TransactionSummary Summary { get; set; }
        = new();

    public IEnumerable<Budget> Budgets { get; set; }
        = new List<Budget>();

    public IEnumerable<Transaction> RecentTransactions { get; set; }
        = new List<Transaction>();
}