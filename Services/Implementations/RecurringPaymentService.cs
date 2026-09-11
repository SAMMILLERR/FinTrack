using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class RecurringPaymentService
    : IRecurringPaymentService
{
    private readonly IRecurringPaymentRepository
        _recurringPaymentRepository;

    public RecurringPaymentService(
        IRecurringPaymentRepository recurringPaymentRepository)
    {
        _recurringPaymentRepository =
            recurringPaymentRepository;
    }

    public async Task<IEnumerable<RecurringPayment>>
        GetRecurringPaymentsAsync(int userId)
    {
        return await _recurringPaymentRepository
            .GetByUserIdAsync(userId);
    }

    public async Task<RecurringPayment?>
        GetRecurringPaymentAsync(
            int recurringPaymentId,
            int userId)
    {
        return await _recurringPaymentRepository
            .GetByIdAsync(
                recurringPaymentId,
                userId);
    }

    public async Task AddRecurringPaymentAsync(
        RecurringPayment recurringPayment)
    {
        recurringPayment.Frequency =
            NormalizeFrequency(
                recurringPayment.Frequency);

        recurringPayment.StartDate =
            recurringPayment.StartDate.Date;

        recurringPayment.NextPaymentDate =
            recurringPayment.StartDate;

        recurringPayment.IsActive = true;

        await _recurringPaymentRepository.AddAsync(
            recurringPayment);
    }

    public async Task UpdateRecurringPaymentAsync(
        RecurringPayment recurringPayment)
    {
        recurringPayment.Frequency =
            NormalizeFrequency(
                recurringPayment.Frequency);

        recurringPayment.StartDate =
            recurringPayment.StartDate.Date;

        recurringPayment.NextPaymentDate =
            recurringPayment.NextPaymentDate.Date;

        await _recurringPaymentRepository.UpdateAsync(
            recurringPayment);
    }

    public async Task ToggleStatusAsync(
        int recurringPaymentId,
        int userId)
    {
        await _recurringPaymentRepository
            .ToggleStatusAsync(
                recurringPaymentId,
                userId);
    }

    public async Task ProcessDuePaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        var duePayments =
            await _recurringPaymentRepository
                .GetDuePaymentsAsync(today);

        foreach (var payment in duePayments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var occurrenceDate =
                payment.NextPaymentDate.Date;

            /*
             * A payment may have been missed while the
             * application was stopped.
             *
             * Therefore process every missed occurrence
             * until the schedule catches up.
             */
            while (
                occurrenceDate <= today &&
                payment.IsActive)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nextPaymentDate =
                    CalculateNextPaymentDate(
                        occurrenceDate,
                        payment.Frequency);

                var shouldRemainActive =
                    payment.EndDate == null ||
                    nextPaymentDate.Date <=
                        payment.EndDate.Value.Date;

                await _recurringPaymentRepository
                    .ProcessOccurrenceAsync(
                        payment,
                        occurrenceDate,
                        nextPaymentDate,
                        shouldRemainActive);

                if (!shouldRemainActive)
                {
                    break;
                }

                occurrenceDate =
                    nextPaymentDate.Date;
            }
        }
    }

    private static DateTime CalculateNextPaymentDate(
        DateTime currentDate,
        string frequency)
    {
        return frequency switch
        {
            "Weekly" =>
                currentDate.AddDays(7),

            "Monthly" =>
                currentDate.AddMonths(1),

            "Yearly" =>
                currentDate.AddYears(1),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported recurring payment frequency: {frequency}")
        };
    }

    private static string NormalizeFrequency(
        string frequency)
    {
        return frequency.Trim().ToLowerInvariant() switch
        {
            "weekly" => "Weekly",
            "monthly" => "Monthly",
            "yearly" => "Yearly",

            _ =>
                throw new ArgumentException(
                    "Frequency must be Weekly, Monthly or Yearly.")
        };
    }
}