using FinTrack.Models;

namespace FinTrack.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    Task AddAsync(Category category);

    Task UpdateAsync(Category category);
    Task<IEnumerable<CategoryType>> GetCategoryTypesAsync();

    Task<IEnumerable<Category>> GetByTypeAsync(string type);

    Task DeleteAsync(int id);
}