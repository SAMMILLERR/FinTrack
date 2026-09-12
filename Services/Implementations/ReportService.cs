using System.Text;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(
        IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }


    // ============================================================
    // EXISTING CURRENT-MONTH REPORT
    // ============================================================

    public async Task<IEnumerable<CategoryReport>>
        GetCurrentMonthReportAsync(int userId)
    {
        return await _reportRepository.GetCategoryReportAsync(
            userId,
            DateTime.Today.Month,
            DateTime.Today.Year);
    }


    // ============================================================
    // EXISTING CSV EXPORT
    // ============================================================

    public async Task<string>
        ExportCurrentMonthReportAsync(int userId)
    {
        var report =
            await GetCurrentMonthReportAsync(userId);

        var builder = new StringBuilder();

        builder.AppendLine(
            "Category,TotalAmount");

        foreach (var item in report)
        {
            var category =
                item.CategoryName
                    .Replace("\"", "\"\"");

            builder.AppendLine(
                $"\"{category}\",{item.TotalAmount}");
        }

        return builder.ToString();
    }


    // ============================================================
    // NEW FILTERED REPORT
    // ============================================================

    public async Task<FinancialReport>
        GenerateReportAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate,
            string transactionFilter)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new ArgumentException(
                "From date cannot be after To date.");
        }

        transactionFilter =
            transactionFilter switch
            {
                "Income" => "Income",

                "Expense" => "Expense",

                _ => "Both"
            };


        var categories =
            (
                await _reportRepository
                    .GetCategoryReportAsync(
                        userId,
                        fromDate,
                        toDate,
                        transactionFilter)
            ).ToList();


        var summary =
            await _reportRepository
                .GetReportSummaryAsync(
                    userId,
                    fromDate,
                    toDate,
                    transactionFilter);


        var total =
            categories.Sum(x => x.Amount);


        if (total > 0)
        {
            foreach (var category in categories)
            {
                category.Percentage =
                    Math.Round(
                        category.Amount /
                        total *
                        100,
                        1);
            }
        }


        return new FinancialReport
        {
            FromDate = fromDate.Date,

            ToDate = toDate.Date,

            TransactionFilter =
                transactionFilter,

            TotalIncome =
                summary.TotalIncome,

            TotalExpense =
                summary.TotalExpense,

            TransactionCount =
                summary.TransactionCount,

            Categories =
                categories
        };
    }
}