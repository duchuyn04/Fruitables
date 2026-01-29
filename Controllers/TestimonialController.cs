using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

public class TestimonialController : Controller
{
    private readonly ITestimonialService _testimonialService;
    private readonly ICartService _cartService;

    public TestimonialController(ITestimonialService testimonialService, ICartService cartService)
    {
        _testimonialService = testimonialService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);
        var testimonials = await _testimonialService.GetActiveTestimonialsAsync();
        return View(testimonials);
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
