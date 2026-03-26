using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IShippingService _shippingService;
    private readonly ICouponService _couponService;

    public CartController(ICartService cartService, IShippingService shippingService, ICouponService couponService)
    {
        _cartService = cartService;
        _shippingService = shippingService;
        _couponService = couponService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionId = GetSessionId();
        var cart = await _cartService.GetCartAsync(sessionId);
        ViewBag.CartCount = cart.Items.Sum(i => i.Quantity);
        return View(cart);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var sessionId = GetSessionId();
        await _cartService.AddToCartAsync(sessionId, productId, quantity);
        TempData["Success"] = "Product added to cart!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        var sessionId = GetSessionId();
        await _cartService.UpdateQuantityAsync(sessionId, productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var sessionId = GetSessionId();
        await _cartService.RemoveFromCartAsync(sessionId, productId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantityAjax([FromBody] UpdateQuantityRequest request)
    {
        var sessionId = GetSessionId();
        await _cartService.UpdateQuantityAsync(sessionId, request.ProductId, request.Quantity);
        var cart = await _cartService.GetCartAsync(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        
        return Json(new
        {
            success = true,
            itemTotal = item?.Total ?? 0,
            actualQuantity = item?.Quantity ?? 0,
            stockQuantity = item?.StockQuantity ?? 0,
            subtotal = cart.Subtotal,
            shippingFee = cart.ShippingFee,
            discount = cart.Discount,
            total = cart.Total,
            cartCount = cart.Items.Sum(i => i.Quantity),
            itemRemoved = item == null,
            maxReached = item != null && item.Quantity >= item.StockQuantity,
            shippingInfo = cart.ShippingInfo != null ? new
            {
                shippingFee = cart.ShippingInfo.ShippingFee,
                zone = (int)cart.ShippingInfo.Zone,
                isFreeShipping = cart.ShippingInfo.IsFreeShipping,
                isReducedShipping = cart.ShippingInfo.IsReducedShipping,
                amountToFreeShipping = cart.ShippingInfo.AmountToFreeShipping,
                message = cart.ShippingInfo.Message
            } : null
        });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCartAjax([FromBody] RemoveFromCartRequest request)
    {
        var sessionId = GetSessionId();
        await _cartService.RemoveFromCartAsync(sessionId, request.ProductId);
        var cart = await _cartService.GetCartAsync(sessionId);
        
        return Json(new
        {
            success = true,
            subtotal = cart.Subtotal,
            shippingFee = cart.ShippingFee,
            discount = cart.Discount,
            total = cart.Total,
            cartCount = cart.Items.Sum(i => i.Quantity),
            isEmpty = !cart.Items.Any(),
            shippingInfo = cart.ShippingInfo != null ? new
            {
                shippingFee = cart.ShippingInfo.ShippingFee,
                zone = (int)cart.ShippingInfo.Zone,
                isFreeShipping = cart.ShippingInfo.IsFreeShipping,
                isReducedShipping = cart.ShippingInfo.IsReducedShipping,
                amountToFreeShipping = cart.ShippingInfo.AmountToFreeShipping,
                message = cart.ShippingInfo.Message
            } : null
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> CalculateShippingAjax([FromBody] CalculateShippingRequest request)
    {
        var shippingInfo = await _shippingService.CalculateShippingAsync(request.Subtotal, request.District ?? string.Empty);
        
        return Json(new
        {
            success = true,
            shippingInfo = new
            {
                shippingFee = shippingInfo.ShippingFee,
                zone = (int)shippingInfo.Zone,
                isFreeShipping = shippingInfo.IsFreeShipping,
                isReducedShipping = shippingInfo.IsReducedShipping,
                amountToFreeShipping = shippingInfo.AmountToFreeShipping,
                message = shippingInfo.Message
            }
        });
    }

    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveFromCartRequest
    {
        public int ProductId { get; set; }
    }
    
    public class CalculateShippingRequest
    {
        public decimal Subtotal { get; set; }
        public string? District { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCouponAjax([FromBody] ApplyCouponAjaxRequest request)
    {
        var sessionId = GetSessionId();
        var result = await _cartService.ApplyCouponAsync(sessionId, request.CouponCode ?? "");

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        var cart = await _cartService.GetCartAsync(sessionId);

        return Json(new
        {
            success      = true,
            message      = result.Message,
            couponCode   = result.CouponCode,
            discount     = cart.Discount,
            subtotal     = cart.Subtotal,
            shippingFee  = cart.ShippingFee,
            total        = cart.Total
        });
    }

    public class ApplyCouponAjaxRequest
    {
        public string? CouponCode { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode, string? returnUrl = null)
    {
        var sessionId = GetSessionId();
        var result = await _cartService.ApplyCouponAsync(sessionId, couponCode);

        if (result.Success)
            TempData["CouponSuccess"] = result.Message;
        else
            TempData["CouponError"] = result.ErrorMessage;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCoupon(string? returnUrl = null)
    {
        var sessionId = GetSessionId();
        await _cartService.RemoveCouponAsync(sessionId);
        TempData["CouponSuccess"] = "Đã bỏ mã giảm giá";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableCoupons()
    {
        var sessionId = GetSessionId();
        var cart = await _cartService.GetCartAsync(sessionId);
        var subtotal = cart.Subtotal;
        var itemCount = cart.Items.Sum(i => i.Quantity);

        var coupons = await _couponService.GetAvailableCouponsAsync(subtotal, itemCount);

        return Json(coupons.Select(c => new
        {
            id             = c.Id,
            code           = c.Code,
            type           = c.Type == Fruitables.Models.CouponType.Percentage ? "percent" : "fixed",
            value          = c.Value,
            discountAmount = c.DiscountAmount,
            minOrderAmount = c.MinOrderAmount,
            minQuantity    = c.MinQuantity,
            endDate        = c.EndDate?.ToString("dd/MM/yyyy"),
            isEligible     = c.IsEligible,
            reason         = c.IneligibleReason
        }));
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
