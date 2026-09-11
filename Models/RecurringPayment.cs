using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class RecurringPayment
{
    public int RecurringPaymentId { get; set; }

    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Payment name is required")]
    [StringLength(100)]
    public string PaymentName { get; set; } = "";

    [Required]
    [Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    [Required]
    public string Frequency { get; set; } = "Monthly";

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime NextPaymentDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public string CategoryName { get; set; } = "";
    public string TransactionType { get; set; } = "";
}