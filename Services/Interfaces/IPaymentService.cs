using FinTrack.Models.Payments;

namespace FinTrack.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request);
}