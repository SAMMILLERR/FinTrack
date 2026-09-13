namespace FinTrack.Models;

public class BudgetRecommendation
{
    public int BudgetId { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public decimal CurrentBudget { get; set; }

    public decimal RecentAverage { get; set; }

    public decimal FirstMonthSpending { get; set; }

    public decimal LatestMonthSpending { get; set; }

    public decimal SpendingIncreasePercentage { get; set; }

    public decimal SuggestedBudget { get; set; }

    public int MonthsAnalyzed { get; set; }

    public bool HasRecommendation { get; set; }

    public string Message { get; set; } = "";

    public string FormattedIncrease =>
        $"{SpendingIncreasePercentage:N1}%";
}