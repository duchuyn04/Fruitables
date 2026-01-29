using Fruitables.Models;

namespace Fruitables.Repositories.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByIdWithChildrenAsync(int id);
    Task<Category?> GetByIdWithProductsAsync(int id);
    Task<Category?> GetByIdWithAllAsync(int id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<List<Category>> GetAllWithProductsAsync();
    Task<List<Category>> GetRootCategoriesAsync();
    Task<List<Category>> GetDeletedCategoriesAsync();
    Task<int> GetMaxSortOrderAsync(int? parentId);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}
