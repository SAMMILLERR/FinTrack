using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface IRecurringPaymentService
{
    Task<IEnumerable<RecurringPayment>>
        GetRecurringPaymentsAsync(int userId);

    Task<RecurringPayment?>
        GetRecurringPaymentAsync(
            int recurringPaymentId,
            int userId);

    Task AddRecurringPaymentAsync(
        RecurringPayment recurringPayment);

    Task UpdateRecurringPaymentAsync(
        RecurringPayment recurringPayment);

    Task ToggleStatusAsync(
        int recurringPaymentId,
        int userId);

    Task ProcessDuePaymentsAsync(
        CancellationToken cancellationToken = default);
}