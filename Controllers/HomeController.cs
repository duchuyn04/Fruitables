using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ITestimonialService _testimonialService;
    private readonly ICartService _cartService;

    public HomeController(
        IProductService productService,
        ICategoryService categoryService,
        ITestimonialService testimonialService,
        ICartService cartService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _testimonialService = testimonialService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);
        ViewBag.AllProducts = await _productService.GetAllProductsAsync();
        // Lấy categories cha để hiển thị tabs ở home
        ViewBag.Categories = await _categoryService.GetParentCategoriesAsync();
        ViewBag.Testimonials = await _testimonialService.GetActiveTestimonialsAsync();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("404")]
    public new async Task<IActionResult> NotFound()
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
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
