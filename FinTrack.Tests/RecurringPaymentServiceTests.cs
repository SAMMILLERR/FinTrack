using FinTrack.Models;
using FinTrack.Models.Payments;
using FinTrack.Repositories;
using FinTrack.Services.Implementations;

namespace FinTrack.Tests;

public class RecurringPaymentServiceTests
{
    [Fact]
    public async Task ProcessDuePaymentsAsync_UsesStableOccurrenceKeyAndAdvancesSchedule()
    {
        var occurrenceDate = DateTime.Today;
        var recurringPayment = CreateDuePayment(occurrenceDate);
        var recurringRepository = new FakeRecurringPaymentRepository(
            recurringPayment);
        var paymentRepository = new RecordingPaymentRepository(
            new PaymentResult
            {
                Success = true,
                Status = PaymentStatus.Completed
            });

        var service = new RecurringPaymentService(
            recurringRepository,
            new PaymentService(paymentRepository));

        await service.ProcessDuePaymentsAsync();

        var request = Assert.Single(paymentRepository.Requests);

        Assert.Equal("recurring:42:" + occurrenceDate.ToString("yyyyMMdd"),
            request.IdempotencyKey);
        Assert.Equal(PaymentType.Recurring, request.PaymentType);
        Assert.Equal(42, request.RecurringPaymentId);
        Assert.Equal(1, request.FromUserId);
        Assert.Equal(2, request.ToUserId);

        var updated = Assert.Single(recurringRepository.UpdatedPayments);
        Assert.Equal(occurrenceDate.AddMonths(1), updated.NextPaymentDate);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task ProcessDuePaymentsAsync_DoesNotAdvanceScheduleWhenPaymentFails()
    {
        var recurringPayment = CreateDuePayment(DateTime.Today);
        var recurringRepository = new FakeRecurringPaymentRepository(
            recurringPayment);
        var paymentRepository = new RecordingPaymentRepository(
            new PaymentResult
            {
                Success = false,
                Status = PaymentStatus.Failed,
                Message = "Insufficient balance."
            });

        var service = new RecurringPaymentService(
            recurringRepository,
            new PaymentService(paymentRepository));

        await service.ProcessDuePaymentsAsync();

        Assert.Single(paymentRepository.Requests);
        Assert.Empty(recurringRepository.UpdatedPayments);
        Assert.Equal(DateTime.Today, recurringPayment.NextPaymentDate);
    }

    private static RecurringPayment CreateDuePayment(
        DateTime occurrenceDate)
    {
        return new RecurringPayment
        {
            RecurringPaymentId = 42,
            UserId = 1,
            ToUserId = 2,
            CategoryId = 10,
            PaymentName = "Rent",
            Amount = 500m,
            Frequency = "Monthly",
            StartDate = occurrenceDate,
            NextPaymentDate = occurrenceDate,
            IsActive = true
        };
    }

    private sealed class RecordingPaymentRepository : IPaymentRepository
    {
        private readonly PaymentResult _result;

        public RecordingPaymentRepository(PaymentResult result)
        {
            _result = result;
        }

        public List<PaymentRequest> Requests { get; } = new();

        public Task<PaymentResult> ProcessTransferAsync(
            PaymentRequest request)
        {
            Requests.Add(request);

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeRecurringPaymentRepository
        : IRecurringPaymentRepository
    {
        private readonly RecurringPayment _duePayment;

        public FakeRecurringPaymentRepository(RecurringPayment duePayment)
        {
            _duePayment = duePayment;
        }

        public List<RecurringPayment> UpdatedPayments { get; } = new();

        public Task<IEnumerable<RecurringPayment>> GetByUserIdAsync(
            int userId) =>
            Task.FromResult<IEnumerable<RecurringPayment>>(
                new[] { _duePayment });

        public Task<RecurringPayment?> GetByIdAsync(
            int recurringPaymentId,
            int userId) =>
            Task.FromResult<RecurringPayment?>(_duePayment);

        public Task<IEnumerable<RecurringPayment>> GetDuePaymentsAsync(
            DateTime today) =>
            Task.FromResult<IEnumerable<RecurringPayment>>(
                _duePayment.IsActive &&
                _duePayment.NextPaymentDate.Date <= today.Date
                    ? new[] { _duePayment }
                    : Array.Empty<RecurringPayment>());

        public Task AddAsync(RecurringPayment recurringPayment) =>
            Task.CompletedTask;

        public Task UpdateAsync(RecurringPayment recurringPayment)
        {
            UpdatedPayments.Add(new RecurringPayment
            {
                RecurringPaymentId = recurringPayment.RecurringPaymentId,
                NextPaymentDate = recurringPayment.NextPaymentDate,
                IsActive = recurringPayment.IsActive
            });

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int recurringPaymentId, int userId) =>
            Task.CompletedTask;

        public Task ToggleStatusAsync(int recurringPaymentId, int userId) =>
            Task.CompletedTask;

        public Task<bool> ProcessOccurrenceAsync(
            RecurringPayment recurringPayment,
            DateTime transactionDate,
            DateTime nextPaymentDate,
            bool isActive) =>
            Task.FromResult(false);
    }
}
