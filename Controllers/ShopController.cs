using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using System.Security.Claims;

namespace Fruitables.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ICartService _cartService;
    private readonly IReviewService _reviewService;

    public ShopController(
        IProductService productService,
        ICategoryService categoryService,
        ICartService cartService,
        IReviewService reviewService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _cartService = cartService;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index(int? categoryId, string? search, decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1)
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);

        var viewModel = await _productService.GetShopViewModelAsync(categoryId, search, minPrice, maxPrice, sortBy, page, 9);
        return View(viewModel);
    }


    // Backward compatibility: redirect /Shop/Detail/7 → /Shop/Detail/ten-san-pham
    [Route("Shop/Detail/{id:int}")]
    public async Task<IActionResult> DetailById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return RedirectPermanent($"/Shop/Detail/{product.Slug}");
    }

    [Route("Shop/Detail/{slug}")]

    public async Task<IActionResult> Detail(string? slug, int page = 1, string? sortBy = null)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();

        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);

        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null) return NotFound();

        var id = product.Id; // Get ID for other services

        ViewBag.RelatedProducts = await _productService.GetRelatedProductsAsync(id, 4);
        // Lấy cây categories đầy đủ để hiển thị ở sidebar
        ViewBag.CategoryTree = await _categoryService.GetCategoryTreeAsync();
        ViewBag.FeaturedProducts = await _productService.GetFeaturedProductsAsync(3);

        // Load review data
        var userId = GetCurrentUserId();
        
        // Get review statistics
        var statistics = await _reviewService.GetProductReviewStatisticsAsync(id);
        ViewBag.ReviewStatistics = statistics;

        // Parse sortBy string to enum
        ReviewSortBy sortByEnum = ReviewSortBy.Newest;
        if (!string.IsNullOrEmpty(sortBy) && Enum.TryParse<ReviewSortBy>(sortBy, true, out var parsedSort))
        {
            sortByEnum = parsedSort;
        }

        // Get reviews with pagination
        var reviewFilter = new ReviewFilterDto
        {
            ProductId = id,
            Page = page,
            PageSize = 10,
            SortBy = sortByEnum
        };
        var reviews = await _reviewService.GetProductReviewsAsync(reviewFilter, userId);
        ViewBag.Reviews = reviews;

        // Check if user can review
        ViewBag.CanReview = false;
        if (userId > 0)
        {
            ViewBag.CanReview = await _reviewService.CanUserReviewProductAsync(userId, id);
        }

        return View(product);
    }

    private string GetSessionId()
    {
        var sessionId = HttpContext.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
        }
        return sessionId;
    }

    private int GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }
        return 0;
    }
}
