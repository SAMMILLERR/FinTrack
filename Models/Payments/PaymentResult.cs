namespace FinTrack.Models.Payments;

public class PaymentResult
{
    public bool Success { get; set; }

    public PaymentStatus Status { get; set; }

    public string? Message { get; set; }

    public int? PaymentId { get; set; }

    public int? SenderTransactionId { get; set; }

    public int? ReceiverTransactionId { get; set; }

    public decimal SenderBalance { get; set; }

    public decimal ReceiverBalance { get; set; }
}