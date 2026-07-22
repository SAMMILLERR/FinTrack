namespace FinTrack.Models;

public class TransactionSummary
{
    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal Balance
    {
        get
        {
            return TotalIncome - TotalExpense;
        }
    }
}