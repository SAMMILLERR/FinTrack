using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IRecurringPaymentRepository
{
    Task<IEnumerable<RecurringPayment>> GetByUserIdAsync(
        int userId);

    Task<RecurringPayment?> GetByIdAsync(
        int recurringPaymentId,
        int userId);

    Task<IEnumerable<RecurringPayment>> GetDuePaymentsAsync(
        DateTime today);

    Task AddAsync(
        RecurringPayment recurringPayment);

    Task UpdateAsync(
        RecurringPayment recurringPayment);

    Task DeleteAsync(
        int recurringPaymentId,
        int userId);

    Task ToggleStatusAsync(
        int recurringPaymentId,
        int userId);

    Task<bool> ProcessOccurrenceAsync(
        RecurringPayment recurringPayment,
        DateTime transactionDate,
        DateTime nextPaymentDate,
        bool isActive);
}