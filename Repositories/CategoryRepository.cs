using Microsoft.EntityFrameworkCore;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;

namespace Fruitables.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetByIdWithChildrenAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetByIdWithProductsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetByIdWithAllAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        return await _dbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    public async Task<List<Category>> GetAllWithProductsAsync()
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .Include(c => c.Products)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<List<Category>> GetRootCategoriesAsync()
    {
        return await _dbSet
            .Where(c => c.ParentId == null && !c.IsDeleted)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<List<Category>> GetDeletedCategoriesAsync()
    {
        return await _dbSet
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync();
    }

    public async Task<int> GetMaxSortOrderAsync(int? parentId)
    {
        var maxSortOrder = await _dbSet
            .Where(c => c.ParentId == parentId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;
        return maxSortOrder;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Slug == slug);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}
