using FinTrack.Models;

namespace FinTrack.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetCategoriesAsync();

    Task<Category?> GetCategoryAsync(int categoryId);

    Task<IEnumerable<CategoryType>> GetCategoryTypesAsync();

    Task AddCategoryAsync(Category category);

    Task UpdateCategoryAsync(Category category);

    Task DeleteCategoryAsync(int categoryId);

    Task ToggleStatusAsync(int categoryId);
}