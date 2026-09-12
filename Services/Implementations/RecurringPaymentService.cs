using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class RecurringPaymentService : IRecurringPaymentService
{
    private readonly IRecurringPaymentRepository _recurringPaymentRepository;

    public RecurringPaymentService(
        IRecurringPaymentRepository recurringPaymentRepository)
    {
        _recurringPaymentRepository = recurringPaymentRepository;
    }

    // ============================================================
    // GET ALL RECURRING PAYMENTS FOR USER
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>> GetRecurringPaymentsAsync(
        int userId)
    {
        return await _recurringPaymentRepository
            .GetByUserIdAsync(userId);
    }

    // ============================================================
    // GET SINGLE RECURRING PAYMENT
    // ============================================================

    public async Task<RecurringPayment?> GetRecurringPaymentAsync(
        int recurringPaymentId,
        int userId)
    {
        return await _recurringPaymentRepository
            .GetByIdAsync(recurringPaymentId, userId);
    }

    // ============================================================
    // ADD RECURRING PAYMENT
    // ============================================================

    public async Task AddRecurringPaymentAsync(
        RecurringPayment recurringPayment)
    {
        recurringPayment.Frequency =
            NormalizeFrequency(recurringPayment.Frequency);

        recurringPayment.StartDate =
            recurringPayment.StartDate.Date;

        recurringPayment.NextPaymentDate =
            recurringPayment.StartDate;

        if (recurringPayment.EndDate.HasValue)
        {
            recurringPayment.EndDate =
                recurringPayment.EndDate.Value.Date;
        }

        recurringPayment.IsActive = true;

        // Save the recurring schedule.
        // Repository must populate RecurringPaymentId.
        await _recurringPaymentRepository
            .AddAsync(recurringPayment);

        /*
         * If the payment is already due,
         * process it immediately instead of
         * waiting for the background worker.
         */
        if (recurringPayment.NextPaymentDate <= DateTime.Today)
        {
            await ProcessDuePaymentsAsync();
        }
    }

    // ============================================================
    // UPDATE RECURRING PAYMENT
    // ============================================================

    public async Task UpdateRecurringPaymentAsync(
        RecurringPayment recurringPayment)
    {
        recurringPayment.Frequency =
            NormalizeFrequency(recurringPayment.Frequency);

        recurringPayment.StartDate =
            recurringPayment.StartDate.Date;

        recurringPayment.NextPaymentDate =
            recurringPayment.NextPaymentDate.Date;

        if (recurringPayment.EndDate.HasValue)
        {
            recurringPayment.EndDate =
                recurringPayment.EndDate.Value.Date;
        }

        await _recurringPaymentRepository
            .UpdateAsync(recurringPayment);

        /*
         * If the updated schedule is already due,
         * process it immediately.
         */
        if (
            recurringPayment.IsActive &&
            recurringPayment.NextPaymentDate <= DateTime.Today)
        {
            await ProcessDuePaymentsAsync();
        }
    }

    // ============================================================
    // PAUSE / RESUME RECURRING PAYMENT
    // ============================================================

    public async Task ToggleStatusAsync(
        int recurringPaymentId,
        int userId)
    {
        await _recurringPaymentRepository
            .ToggleStatusAsync(
                recurringPaymentId,
                userId);
    }

    // ============================================================
    // PROCESS ALL DUE RECURRING PAYMENTS
    // ============================================================

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
             * Catch up missed payments.
             *
             * Example:
             *
             * NextPaymentDate = June 1
             * Today            = September 12
             *
             * The service processes:
             *
             * June 1
             * July 1
             * August 1
             * September 1
             *
             * and advances the schedule.
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

                /*
                 * The schedule remains active only when
                 * the NEXT occurrence is still within
                 * the configured EndDate.
                 */
                var shouldRemainActive =
                    !payment.EndDate.HasValue ||
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

    // ============================================================
    // CALCULATE NEXT PAYMENT DATE
    // ============================================================

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

    // ============================================================
    // NORMALIZE FREQUENCY
    // ============================================================

    private static string NormalizeFrequency(
        string frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            throw new ArgumentException(
                "Frequency is required.");
        }

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