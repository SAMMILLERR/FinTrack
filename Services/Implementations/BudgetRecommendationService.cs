using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class BudgetRecommendationService
    : IBudgetRecommendationService
{
    private const decimal SafetyBufferPercentage = 10m;
    private const decimal RoundingUnit = 100m;

    private readonly IBudgetRepository _budgetRepository;
    private readonly IBudgetService _budgetService;

    public BudgetRecommendationService(
        IBudgetRepository budgetRepository,
        IBudgetService budgetService)
    {
        _budgetRepository = budgetRepository;
        _budgetService = budgetService;
    }

    // =========================================================
    // CALCULATE AUTOMATIC BUDGET
    // =========================================================

    public async Task<BudgetRecommendation>
        GetRecommendationAsync(
            int userId,
            Budget budget)
    {
        var recommendation = new BudgetRecommendation
        {
            BudgetId = budget.BudgetId,
            CategoryId = budget.CategoryId,
            CategoryName = budget.CategoryName,
            CurrentBudget = budget.LimitAmount
        };

        var today = DateTime.Today;

        var currentMonth = new DateTime(
            today.Year,
            today.Month,
            1);

        var budgetMonth = new DateTime(
            budget.BudgetYear,
            budget.BudgetMonth,
            1);

        // ---------------------------------------------------------
        // Only automatically adjust the current month's budget
        // ---------------------------------------------------------

        if (budgetMonth != currentMonth)
        {
            recommendation.Message =
                "Automatic budget adjustment is only performed "
                + "for the current month.";

            return recommendation;
        }

        // ---------------------------------------------------------
        // Get ALL historical completed-month expenses
        //
        // Current month is excluded because it is incomplete.
        // ---------------------------------------------------------

        var history =
            await _budgetRepository
                .GetRecentMonthlySpendingAsync(
                    userId,
                    budget.CategoryId,
                    new DateTime(1900, 1, 1),
                    currentMonth);

        if (history.Count == 0)
        {
            recommendation.Message =
                "There is not enough completed expense history "
                + "to calculate an automatic budget.";

            return recommendation;
        }

        // ---------------------------------------------------------
        // Find first month in which this category had spending
        // ---------------------------------------------------------

        var firstMonth =
            history.Min(x =>
                new DateTime(
                    x.MonthStart.Year,
                    x.MonthStart.Month,
                    1));

        // ---------------------------------------------------------
        // Build every month between first expense month
        // and the previous completed month.
        //
        // Missing months are treated as ₹0.
        // ---------------------------------------------------------

        var monthsAnalyzed =
            (
                (currentMonth.Year - firstMonth.Year) * 12
                +
                currentMonth.Month -
                firstMonth.Month
            );

        if (monthsAnalyzed <= 0)
        {
            recommendation.Message =
                "There is not enough completed expense history "
                + "to calculate an automatic budget.";

            return recommendation;
        }

        var monthlyAmounts =
            Enumerable
                .Range(0, monthsAnalyzed)
                .Select(offset =>
                {
                    var month =
                        firstMonth.AddMonths(offset);

                    var record =
                        history.FirstOrDefault(
                            x =>
                                x.MonthStart.Year == month.Year
                                &&
                                x.MonthStart.Month == month.Month);

                    return record?.Amount ?? 0m;
                })
                .ToList();

        recommendation.MonthsAnalyzed =
            monthlyAmounts.Count;

        recommendation.FirstMonthSpending =
            monthlyAmounts.First();

        recommendation.LatestMonthSpending =
            monthlyAmounts.Last();

        recommendation.RecentAverage =
            monthlyAmounts.Average();

        // ---------------------------------------------------------
        // Calculate spending change
        // ---------------------------------------------------------

        if (recommendation.FirstMonthSpending > 0)
        {
            recommendation.SpendingIncreasePercentage =
                (
                    (
                        recommendation.LatestMonthSpending
                        -
                        recommendation.FirstMonthSpending
                    )
                    /
                    recommendation.FirstMonthSpending
                )
                * 100m;
        }
        else if (recommendation.LatestMonthSpending > 0)
        {
            recommendation.SpendingIncreasePercentage = 100m;
        }
        else
        {
            recommendation.SpendingIncreasePercentage = 0m;
        }

        // ---------------------------------------------------------
        // Calculate new budget
        //
        // Average monthly spending
        //          +
        //       10% buffer
        //          ↓
        // Round UP to nearest ₹100
        // ---------------------------------------------------------

        var bufferedAmount =
            recommendation.RecentAverage
            *
            (
                1m +
                SafetyBufferPercentage / 100m
            );

        var suggestedBudget =
            RoundUpToNearest(
                bufferedAmount,
                RoundingUnit);

        recommendation.SuggestedBudget =
            suggestedBudget;

        // ---------------------------------------------------------
        // Determine whether current budget should change
        // ---------------------------------------------------------

        recommendation.HasRecommendation =
            suggestedBudget != budget.LimitAmount;

        if (recommendation.HasRecommendation)
        {
            recommendation.Message =
                $"Based on {recommendation.MonthsAnalyzed} "
                + "completed month(s) of expense history, "
                + $"your average {budget.CategoryName} spending "
                + $"is ₹{recommendation.RecentAverage:N0}. "
                + $"With a 10% safety buffer, your budget is "
                + $"automatically adjusted to "
                + $"₹{suggestedBudget:N0}.";
        }
        else
        {
            recommendation.Message =
                $"Your current {budget.CategoryName} budget of "
                + $"₹{budget.LimitAmount:N0} is already aligned "
                + "with your historical spending pattern.";
        }

        return recommendation;
    }

    // =========================================================
    // APPLY AUTOMATIC BUDGET
    // =========================================================

    public async Task<bool>
        ApplyRecommendationAsync(
            int userId,
            int budgetId)
    {
        var budget =
            await _budgetRepository
                .GetByIdAsync(
                    budgetId,
                    userId);

        if (budget == null)
        {
            return false;
        }

        var today = DateTime.Today;

        var currentMonth =
            new DateTime(
                today.Year,
                today.Month,
                1);

        var budgetMonth =
            new DateTime(
                budget.BudgetYear,
                budget.BudgetMonth,
                1);

        // Never modify historical budgets.
        if (budgetMonth != currentMonth)
        {
            return false;
        }

        var recommendation =
            await GetRecommendationAsync(
                userId,
                budget);

        if (!recommendation.HasRecommendation)
        {
            return false;
        }

        // ---------------------------------------------------------
        // Update whether the recommended amount is higher OR lower.
        // The transaction history is the source of truth.
        // ---------------------------------------------------------

        budget.LimitAmount =
            recommendation.SuggestedBudget;

        await _budgetRepository
            .UpdateAsync(budget);

        return true;
    }

    // =========================================================
    // CREATE OR UPDATE CURRENT MONTH BUDGET
    // =========================================================

    public async Task<Budget?>
        SynchronizeCategoryBudgetAsync(
            int userId,
            Category category)
    {
        var today = DateTime.Today;

        var currentMonth =
            new DateTime(
                today.Year,
                today.Month,
                1);

        var budgets =
            await _budgetService
                .GetBudgetsAsync(userId);

        var existingBudget =
            budgets.FirstOrDefault(
                x =>
                    x.CategoryId == category.CategoryId
                    &&
                    x.BudgetMonth == currentMonth.Month
                    &&
                    x.BudgetYear == currentMonth.Year);

        // ---------------------------------------------------------
        // Existing current-month budget
        // ---------------------------------------------------------

        if (existingBudget != null)
        {
            var recommendation =
                await GetRecommendationAsync(
                    userId,
                    existingBudget);

            if (recommendation.HasRecommendation)
            {
                existingBudget.LimitAmount =
                    recommendation.SuggestedBudget;

                await _budgetService
                    .UpdateBudgetAsync(existingBudget);
            }

            return existingBudget;
        }

        // ---------------------------------------------------------
        // No current-month budget.
        //
        // Create one only if there is historical expense data.
        // ---------------------------------------------------------

        var temporaryBudget =
            new Budget
            {
                UserId = userId,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                BudgetMonth = currentMonth.Month,
                BudgetYear = currentMonth.Year,
                LimitAmount = 0
            };

        var recommendationForNewBudget =
            await GetRecommendationAsync(
                userId,
                temporaryBudget);

        if (recommendationForNewBudget.SuggestedBudget <= 0)
        {
            return null;
        }

        temporaryBudget.LimitAmount =
            recommendationForNewBudget.SuggestedBudget;

        await _budgetService
            .AddBudgetAsync(temporaryBudget);

        return temporaryBudget;
    }

    // =========================================================
    // ROUND UP
    // =========================================================

    private static decimal RoundUpToNearest(
        decimal value,
        decimal unit)
    {
        if (unit <= 0)
        {
            return value;
        }

        return Math.Ceiling(
            value / unit
        ) * unit;
    }
}