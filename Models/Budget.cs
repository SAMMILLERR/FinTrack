using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Budget
{
    public int BudgetId { get; set; }

    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    [Required]
    [Range(1, 12)]
    public int BudgetMonth { get; set; }

    [Required]
    [Range(2024, 2100)]
    public int BudgetYear { get; set; }

    [Required]
    [Range(1, 100000000)]
    public decimal LimitAmount { get; set; }

    public decimal SpentAmount { get; set; }

    public decimal RemainingAmount
    {
        get
        {
            return LimitAmount - SpentAmount;
        }
    }

    public decimal UsagePercentage
    {
        get
        {
            if (LimitAmount == 0)
                return 0;

            return (SpentAmount / LimitAmount) * 100;
        }
    }

    public DateTime CreatedOn { get; set; }
}