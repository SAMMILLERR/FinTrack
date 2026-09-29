using FinTrack.Models.Payments;
using FinTrack.Repositories;
using FinTrack.Services.Implementations;

namespace FinTrack.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPaymentAsync_RejectsMissingIdempotencyKey()
    {
        var repository = new RecordingPaymentRepository();
        var service = new PaymentService(repository);

        var result = await service.ProcessPaymentAsync(
            new PaymentRequest
            {
                FromUserId = 1,
                ToUserId = 2,
                Amount = 100m,
                ExpenseCategoryId = 10
            });

        Assert.False(result.Success);
        Assert.Equal(PaymentStatus.Failed, result.Status);
        Assert.Equal(
            "A payment idempotency key is required.",
            result.Message);
        Assert.Empty(repository.Requests);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PassesValidRequestToRepository()
    {
        var expected = new PaymentResult
        {
            Success = true,
            Status = PaymentStatus.Completed,
            PaymentId = 99
        };

        var repository = new RecordingPaymentRepository
        {
            Result = expected
        };

        var service = new PaymentService(repository);

        var request = new PaymentRequest
        {
            FromUserId = 1,
            ToUserId = 2,
            Amount = 100m,
            ExpenseCategoryId = 10,
            IdempotencyKey = "payment-request-1"
        };

        var result = await service.ProcessPaymentAsync(request);

        Assert.Same(expected, result);
        Assert.Single(repository.Requests);
        Assert.Same(request, repository.Requests[0]);
    }

    private sealed class RecordingPaymentRepository : IPaymentRepository
    {
        public List<PaymentRequest> Requests { get; } = new();

        public PaymentResult Result { get; set; } = new()
        {
            Success = true,
            Status = PaymentStatus.Completed
        };

        public Task<PaymentResult> ProcessTransferAsync(
            PaymentRequest request)
        {
            Requests.Add(request);

            return Task.FromResult(Result);
        }
    }
}
