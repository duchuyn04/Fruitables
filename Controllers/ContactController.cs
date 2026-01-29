using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

public class ContactController : Controller
{
    private readonly IContactService _contactService;
    private readonly ICartService _cartService;

    public ContactController(IContactService contactService, ICartService cartService)
    {
        _contactService = contactService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string name, string email, string message)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(message))
        {
            TempData["Error"] = "Please fill in all fields.";
            return RedirectToAction(nameof(Index));
        }

        await _contactService.SendMessageAsync(name, email, message);
        TempData["Success"] = "Your message has been sent successfully!";
        return RedirectToAction(nameof(Index));
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
