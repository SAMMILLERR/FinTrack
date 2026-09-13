namespace FinTrack.Models.Payments;

public class PaymentRequest
{
    public int FromUserId { get; set; }

    public int ToUserId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public PaymentType PaymentType { get; set; } = PaymentType.Manual;

    public string? Description { get; set; }

    // Expense category selected by the sender.
    public int ExpenseCategoryId { get; set; }

    // Set only when this payment was created
    // from a recurring payment schedule.
    public int? RecurringPaymentId { get; set; }
}