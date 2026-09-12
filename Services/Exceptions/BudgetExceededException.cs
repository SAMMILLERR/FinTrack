namespace FinTrack.Exceptions;

public class BudgetExceededException : Exception
{
    public decimal BudgetLimit { get; }

    public decimal CurrentSpent { get; }

    public decimal TransactionAmount { get; }

    public decimal RemainingAmount { get; }

    public decimal ExceededBy { get; }

    public BudgetExceededException(
        decimal budgetLimit,
        decimal currentSpent,
        decimal transactionAmount)
        : base("This transaction would exceed the monthly budget.")
    {
        BudgetLimit = budgetLimit;
        CurrentSpent = currentSpent;
        TransactionAmount = transactionAmount;

        RemainingAmount =
            Math.Max(0, budgetLimit - currentSpent);

        ExceededBy =
            (currentSpent + transactionAmount) - budgetLimit;
    }
}