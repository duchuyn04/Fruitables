using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ICartService _cartService;

    public ShopController(
        IProductService productService,
        ICategoryService categoryService,
        ICartService cartService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(int? categoryId, string? search, decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1)
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);

        var viewModel = await _productService.GetShopViewModelAsync(categoryId, search, minPrice, maxPrice, sortBy, page, 9);
        return View(viewModel);
    }

    public async Task<IActionResult> Detail(int? id)
    {
        if (id == null) return NotFound();

        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);

        var product = await _productService.GetProductByIdAsync(id.Value);
        if (product == null) return NotFound();

        ViewBag.RelatedProducts = await _productService.GetRelatedProductsAsync(id.Value, 4);
        ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
        ViewBag.FeaturedProducts = await _productService.GetFeaturedProductsAsync(3);

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
}
