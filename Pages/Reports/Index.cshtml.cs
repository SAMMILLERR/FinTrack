using System.Security.Claims;
using System.Text;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Reports;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReportRepository _repository;

    public IndexModel(IReportRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<CategoryReport> Reports
        = new List<CategoryReport>();

    public async Task OnGetAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        Reports = await _repository.GetCategoryReportAsync(
            userId,
            DateTime.Today.Month,
            DateTime.Today.Year);
    }

    public async Task<FileResult> OnGetExportAsync()
    {
        int userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var report = await _repository.GetCategoryReportAsync(
            userId,
            DateTime.Today.Month,
            DateTime.Today.Year);

        var builder = new StringBuilder();

        builder.AppendLine("Category,TotalAmount");

        foreach (var item in report)
        {
            builder.AppendLine(
                $"{item.CategoryName},{item.TotalAmount}");
        }

        return File(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/csv",
            $"ExpenseReport_{DateTime.Now:yyyyMMdd}.csv");
    }
}