using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Transaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    [Required]
    [Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime TransactionDate { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public string TransactionType { get; set; } = "";

    public DateTime CreatedOn { get; set; }

    public string UserName { get; set; } = "";

    public int? RecurringPaymentId { get; set; }

    // Links a transaction to the payment that created it.
    // NULL = normal/manual transaction.
    // Non-NULL = transaction created as part of a payment.
    public int? PaymentId { get; set; }
}