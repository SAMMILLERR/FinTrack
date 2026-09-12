namespace FinTrack.Models;

public class FinancialReport
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string TransactionFilter { get; set; } = "Both";

    public string ChartType { get; set; } = "Bar";

    public bool IncludeDebtsCredits { get; set; }

    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal NetAmount => TotalIncome - TotalExpense;

    public int TransactionCount { get; set; }

    public IEnumerable<FinancialReportCategory> Categories { get; set; }
        = new List<FinancialReportCategory>();
}

public class FinancialReportCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public string TransactionType { get; set; } = "";

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }
}