using System.Security.Claims;
using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.Pages.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITransactionRepository _repository;
    private readonly ICategoryRepository _categoryRepository;

    private const int PageSize = 10;

    public IndexModel(
        ITransactionRepository repository,
        ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
    }

    public IEnumerable<Transaction> Transactions { get; set; }
        = new List<Transaction>();

    public IEnumerable<Category> Categories { get; set; }
        = new List<Category>();

    public TransactionSummary Summary { get; set; }
        = new();

    // Filters
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    // Pagination
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _categoryRepository.GetAllAsync();

        int? userId = null;

        if (!User.IsInRole("Admin"))
        {
            userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            Summary = await _repository.GetSummaryAsync(userId.Value);
        }

        var totalRecords =
            await _repository.GetSearchCountAsync(
                userId,
                FromDate,
                ToDate,
                CategoryId,
                Type);

        TotalPages =
            (int)Math.Ceiling(totalRecords / (double)PageSize);

        Transactions =
            await _repository.SearchPagedAsync(
                userId,
                FromDate,
                ToDate,
                CategoryId,
                Type,
                CurrentPage,   // <-- FIXED HERE
                PageSize);
    }
}