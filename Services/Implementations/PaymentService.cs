using FinTrack.Models.Payments;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(
        IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request)
    {
        if (request == null)
        {
            return new PaymentResult
            {
                Success = false,
                Status = PaymentStatus.Failed,
                Message = "Payment request is required."
            };
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return new PaymentResult
            {
                Success = false,
                Status = PaymentStatus.Failed,
                Message = "A payment idempotency key is required."
            };
        }

        return await _paymentRepository
            .ProcessTransferAsync(request);
    }
}
