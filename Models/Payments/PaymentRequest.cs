using System.Text.Json.Serialization;

namespace FinTrack.Models.Payments;

public class PaymentRequest
{
    // Sender is determined by the authenticated user on the server.
    // It should NOT be supplied by the browser/client.
    [JsonIgnore]
    public int FromUserId { get; set; }

    public int ToUserId { get; set; }

    public decimal Amount { get; set; }

    // Manual API payments receive their posting time from the server.
    // Interactive Server components and the recurring worker set this internally.
    [JsonIgnore]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    // The public payment API only creates manual payments. Scheduled payments
    // are created by the recurring-payment worker.
    [JsonIgnore]
    public PaymentType PaymentType { get; set; } = PaymentType.Manual;

    public string? Description { get; set; }

    // Expense category selected by the sender.
    public int ExpenseCategoryId { get; set; }

    // Set only when this payment was created
    // from a recurring payment schedule.
    [JsonIgnore]
    public int? RecurringPaymentId { get; set; }

    // API callers provide this through the Idempotency-Key header. Interactive
    // and recurring payments create it internally so a retry cannot post twice.
    [JsonIgnore]
    public string? IdempotencyKey { get; set; }
}
