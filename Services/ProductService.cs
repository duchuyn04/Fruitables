using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _unitOfWork.Products.Query()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync();
    }

    public async Task<List<Product>> GetFeaturedProductsAsync(int count = 8)
    {
        return await _unitOfWork.Products.Query()
            .Where(p => p.IsActive && p.IsFeatured)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _unitOfWork.Products.Query()
            .Where(p => p.IsActive && p.CategoryId == categoryId)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetProductBySlugAsync(string slug)
    {
        return await _unitOfWork.Products.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<ShopViewModel> GetShopViewModelAsync(int? categoryId, string? search, decimal? minPrice, decimal? maxPrice, string? sortBy, int page, int pageSize)
    {
        var query = _unitOfWork.Products.Query()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        // Filter by category
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        // Filter by search
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description!.Contains(search));

        // Filter by price
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // Sorting
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _unitOfWork.Categories.Query()
            .Include(c => c.Products.Where(p => p.IsActive))
            .ToListAsync();

        return new ShopViewModel
        {
            Products = products,
            Categories = categories,
            SelectedCategoryId = categoryId,
            SearchTerm = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sortBy,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };
    }

    public async Task<List<Product>> GetRelatedProductsAsync(int productId, int count = 4)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null) return new List<Product>();

        return await _unitOfWork.Products.Query()
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != productId)
            .Include(p => p.Images)
            .Take(count)
            .ToListAsync();
    }
}
