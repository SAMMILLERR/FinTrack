using FinTrack.Models;
using FinTrack.Models.Payments;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class RecurringPaymentService : IRecurringPaymentService
{
    private readonly IRecurringPaymentRepository _recurringPaymentRepository;
    private readonly IPaymentService _paymentService;

    public RecurringPaymentService(
        IRecurringPaymentRepository recurringPaymentRepository,
        IPaymentService paymentService)
    {
        _recurringPaymentRepository = recurringPaymentRepository;
        _paymentService = paymentService;
    }

    // ============================================================
    // GET ALL
    // ============================================================

    public async Task<IEnumerable<RecurringPayment>>
        GetRecurringPaymentsAsync(int userId)
    {
        return await _recurringPaymentRepository
            .GetByUserIdAsync(userId);
    }

    // ============================================================
    // GET ONE
    // ============================================================

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

    // ============================================================
    // ADD
    // ============================================================

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

        if (recurringPayment.EndDate.HasValue)
        {
            recurringPayment.EndDate =
                recurringPayment.EndDate.Value.Date;
        }

        recurringPayment.IsActive = true;

        await _recurringPaymentRepository
            .AddAsync(recurringPayment);

        // If the payment starts today or in the past,
        // process it immediately.
        if (recurringPayment.NextPaymentDate <= DateTime.Today)
        {
            await ProcessDuePaymentsAsync();
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

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

        if (recurringPayment.EndDate.HasValue)
        {
            recurringPayment.EndDate =
                recurringPayment.EndDate.Value.Date;
        }

        await _recurringPaymentRepository
            .UpdateAsync(recurringPayment);

        if (
            recurringPayment.IsActive &&
            recurringPayment.NextPaymentDate <= DateTime.Today)
        {
            await ProcessDuePaymentsAsync();
        }
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task DeleteRecurringPaymentAsync(
        int recurringPaymentId,
        int userId)
    {
        var recurringPayment =
            await _recurringPaymentRepository
                .GetByIdAsync(
                    recurringPaymentId,
                    userId);

        if (recurringPayment == null)
        {
            return;
        }

        // Delete only the schedule.
        // Existing transactions remain untouched.
        await _recurringPaymentRepository
            .DeleteAsync(
                recurringPaymentId,
                userId);
    }

    // ============================================================
    // PAUSE / RESUME
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
    // PROCESS DUE PAYMENTS
    // ============================================================

    public async Task ProcessDuePaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        var duePayments =
            await _recurringPaymentRepository
                .GetDuePaymentsAsync(today);

        foreach (var recurringPayment in duePayments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var occurrenceDate =
                recurringPayment.NextPaymentDate.Date;

            // -----------------------------------------------------
            // Process every overdue occurrence.
            // -----------------------------------------------------

            while (
                occurrenceDate <= today &&
                recurringPayment.IsActive)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // -------------------------------------------------
                // Validate receiver
                // -------------------------------------------------

                if (recurringPayment.ToUserId <= 0)
                {
                    break;
                }

                if (
                    recurringPayment.ToUserId ==
                    recurringPayment.UserId)
                {
                    break;
                }

                // -------------------------------------------------
                // Calculate next occurrence
                // -------------------------------------------------

                var nextPaymentDate =
                    CalculateNextPaymentDate(
                        occurrenceDate,
                        recurringPayment.Frequency);

                // -------------------------------------------------
                // Determine whether schedule remains active
                // -------------------------------------------------

                var shouldRemainActive =
                    !recurringPayment.EndDate.HasValue ||
                    nextPaymentDate.Date <=
                    recurringPayment.EndDate.Value.Date;

                // -------------------------------------------------
                // BUILD PAYMENT REQUEST
                // -------------------------------------------------

                var paymentRequest = new PaymentRequest
                {
                    FromUserId =
                        recurringPayment.UserId,

                    ToUserId =
                        recurringPayment.ToUserId,

                    Amount =
                        recurringPayment.Amount,

                    PaymentDate =
                        occurrenceDate,

                    PaymentType =
                        PaymentType.Recurring,

                    Description =
                        recurringPayment.PaymentName,

                    ExpenseCategoryId =
                        recurringPayment.CategoryId,

                    RecurringPaymentId =
                        recurringPayment.RecurringPaymentId
                };

                // -------------------------------------------------
                // PROCESS THROUGH PAYMENT ENGINE
                // -------------------------------------------------

                var result =
                    await _paymentService
                        .ProcessPaymentAsync(
                            paymentRequest);

                // -------------------------------------------------
                // PAYMENT FAILED
                // -------------------------------------------------

                if (!result.Success)
                {
                    // Do not advance the schedule.
                    //
                    // The occurrence remains due so that it
                    // can be retried later.
                    break;
                }

                // -------------------------------------------------
                // PAYMENT SUCCESSFUL
                // -------------------------------------------------

                recurringPayment.NextPaymentDate =
                    nextPaymentDate;

                recurringPayment.IsActive =
                    shouldRemainActive;

                // -------------------------------------------------
                // UPDATE RECURRING SCHEDULE
                // -------------------------------------------------

                await _recurringPaymentRepository
                    .UpdateAsync(
                        recurringPayment);

                // -------------------------------------------------
                // END DATE REACHED
                // -------------------------------------------------

                if (!shouldRemainActive)
                {
                    break;
                }

                // -------------------------------------------------
                // MOVE TO NEXT OCCURRENCE
                // -------------------------------------------------

                occurrenceDate =
                    nextPaymentDate.Date;
            }
        }
    }

    // ============================================================
    // CALCULATE NEXT PAYMENT DATE
    // ============================================================

    private static DateTime
        CalculateNextPaymentDate(
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

        return frequency
            .Trim()
            .ToLowerInvariant() switch
        {
            "weekly" =>
                "Weekly",

            "monthly" =>
                "Monthly",

            "yearly" =>
                "Yearly",

            _ =>
                throw new ArgumentException(
                    "Frequency must be Weekly, Monthly or Yearly.")
        };
    }
}