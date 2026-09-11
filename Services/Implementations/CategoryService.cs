using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<Category?> GetCategoryAsync(int categoryId)
    {
        return await _categoryRepository.GetByIdAsync(categoryId);
    }

    public async Task<IEnumerable<CategoryType>> GetCategoryTypesAsync()
    {
        return await _categoryRepository.GetCategoryTypesAsync();
    }

    public async Task AddCategoryAsync(Category category)
    {
        await _categoryRepository.AddAsync(category);
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        await _categoryRepository.UpdateAsync(category);
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        await _categoryRepository.DeleteAsync(categoryId);
    }

    public async Task ToggleStatusAsync(int categoryId)
    {
        await _categoryRepository.ToggleStatusAsync(categoryId);
    }
}