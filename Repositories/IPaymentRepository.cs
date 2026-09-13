using FinTrack.Models.Payments;

namespace FinTrack.Repositories;

public interface IPaymentRepository
{
    Task<PaymentResult> ProcessTransferAsync(
        PaymentRequest request);
}