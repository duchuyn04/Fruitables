using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using Fruitables.Repositories.Interfaces;
using Fruitables.Models;

namespace Fruitables.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IAddressService _addressService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVietnamAddressService _vietnamAddressService;
    private readonly IShippingService _shippingService;
    
    // Session keys for shipping snapshot (Requirements 6.1, 6.2, 6.3)
    private const string ShippingFeeSnapshotKey = "ShippingFeeSnapshot";
    private const string ShippingZoneSnapshotKey = "ShippingZoneSnapshot";
    private const string ShippingSnapshotTimeKey = "ShippingSnapshotTime";
    private const string ShippingDistrictSnapshotKey = "ShippingDistrictSnapshot";

    public CheckoutController(
        ICartService cartService, 
        IOrderService orderService, 
        IAddressService addressService,
        IUnitOfWork unitOfWork,
        IVietnamAddressService vietnamAddressService,
        IShippingService shippingService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _addressService = addressService;
        _unitOfWork = unitOfWork;
        _vietnamAddressService = vietnamAddressService;
        _shippingService = shippingService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionId = GetSessionId();
        
        // Get userId from session/cookie (for now, we'll use a simple approach)
        // In a real app, this would come from authentication
        var userId = GetCurrentUserId();
        
        List<AddressViewModel> addressViewModels = new();
        int? defaultAddressId = null;
        string? defaultDistrict = null;
        
        if (userId.HasValue)
        {
            // Load saved addresses for logged-in user
            var addresses = await _addressService.GetUserAddressesAsync(userId.Value);
            addressViewModels = addresses.Select(a => new AddressViewModel
            {
                Id = a.Id,
                FullName = a.FullName,
                Phone = a.Phone,
                ProvinceCode = a.ProvinceCode,
                ProvinceName = a.ProvinceName,
                DistrictCode = a.DistrictCode,
                DistrictName = a.DistrictName,
                WardCode = a.WardCode,
                WardName = a.WardName,
                StreetAddress = a.StreetAddress,
                IsDefault = a.IsDefault
            }).ToList();
            
            // Get default address ID and district
            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault);
            if (defaultAddress != null)
            {
                defaultAddressId = defaultAddress.Id;
                defaultDistrict = defaultAddress.DistrictName;
            }
        }
        
        // Get cart with shipping calculated based on default district
        var cart = await _cartService.GetCartAsync(sessionId, defaultDistrict);

        if (!cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }
        
        // Create shipping snapshot when entering checkout (Requirements 6.1)
        if (cart.ShippingInfo != null)
        {
            SaveShippingSnapshot(cart.ShippingInfo, defaultDistrict);
        }

        ViewBag.CartCount = cart.Items.Sum(i => i.Quantity);
        ViewBag.Cart = cart;
        ViewBag.SavedAddresses = addressViewModels;
        
        var model = new CheckoutViewModel
        {
            SelectedAddressId = defaultAddressId
        };
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
    {
        var sessionId = GetSessionId();
        
        // Thêm sản phẩm vào giỏ hàng
        await _cartService.AddToCartAsync(sessionId, productId, quantity);
        
        // Chuyển thẳng đến trang checkout
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var sessionId = GetSessionId();
        var userId = GetCurrentUserId();
        
        // Get district from selected address or model
        string? district = null;
        if (model.SelectedAddressId.HasValue)
        {
            var selectedAddress = await _unitOfWork.Addresses.GetByIdAsync(model.SelectedAddressId.Value);
            district = selectedAddress?.DistrictName;
        }
        else
        {
            district = model.DistrictName;
        }
        
        var cart = await _cartService.GetCartAsync(sessionId, district);

        if (!ModelState.IsValid)
        {
            // Reload addresses for display
            if (userId.HasValue)
            {
                var addresses = await _addressService.GetUserAddressesAsync(userId.Value);
                ViewBag.SavedAddresses = addresses.Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    ProvinceCode = a.ProvinceCode,
                    ProvinceName = a.ProvinceName,
                    DistrictCode = a.DistrictCode,
                    DistrictName = a.DistrictName,
                    WardCode = a.WardCode,
                    WardName = a.WardName,
                    StreetAddress = a.StreetAddress,
                    IsDefault = a.IsDefault
                }).ToList();
            }
            else
            {
                ViewBag.SavedAddresses = new List<AddressViewModel>();
            }
            
            ViewBag.CartCount = cart.Items.Sum(i => i.Quantity);
            ViewBag.Cart = cart;
            return View("Index", model);
        }

        // Sanitize StreetAddress to prevent XSS (Requirements 4.3)
        model.StreetAddress = _vietnamAddressService.SanitizeStreetAddress(model.StreetAddress);

        // Handle SaveAddress and SetAsDefault flags
        if (model.SaveAddress && !model.SelectedAddressId.HasValue && userId.HasValue)
        {
            // Compose full address using VietnamAddressService (Requirements 5.1)
            var addressComponents = new AddressComponentsDto
            {
                ProvinceCode = model.ProvinceCode,
                ProvinceName = model.ProvinceName ?? string.Empty,
                DistrictCode = model.DistrictCode,
                DistrictName = model.DistrictName ?? string.Empty,
                WardCode = model.WardCode,
                WardName = model.WardName ?? string.Empty,
                StreetAddress = model.StreetAddress
            };
            
            // User wants to save the new address with all code and name fields (Requirements 5.2)
            var newAddress = new Models.Address
            {
                UserId = userId.Value,
                FullName = model.FullName ?? model.FirstName.Trim(),
                Phone = model.Mobile,
                ProvinceCode = model.ProvinceCode,
                ProvinceName = model.ProvinceName ?? string.Empty,
                DistrictCode = model.DistrictCode,
                DistrictName = model.DistrictName ?? string.Empty,
                WardCode = model.WardCode,
                WardName = model.WardName ?? string.Empty,
                StreetAddress = model.StreetAddress,
                IsDefault = model.SetAsDefault,
                CreatedAt = DateTime.UtcNow
            };
            
            var savedAddress = await _addressService.CreateAddressAsync(newAddress);
            model.SelectedAddressId = savedAddress.Id;
            
            if (model.SetAsDefault)
            {
                await _addressService.SetDefaultAddressAsync(userId.Value, savedAddress.Id);
            }
        }

        // Get shipping fee from snapshot (Requirements 6.2, 6.3)
        var snapshotShippingFee = GetShippingFeeFromSnapshot();
        var snapshotZone = GetShippingZoneFromSnapshot();
        
        // If no snapshot exists, calculate fresh (fallback)
        if (!snapshotShippingFee.HasValue)
        {
            var shippingInfo = await _shippingService.CalculateShippingAsync(cart.Subtotal, district ?? string.Empty);
            snapshotShippingFee = shippingInfo.ShippingFee;
            snapshotZone = shippingInfo.Zone;
        }
        
        // Store snapshot values in model for OrderService to use
        model.Cart = cart;
        model.Cart.ShippingFee = snapshotShippingFee.Value;
        if (model.Cart.ShippingInfo != null)
        {
            model.Cart.ShippingInfo.ShippingFee = snapshotShippingFee.Value;
            model.Cart.ShippingInfo.Zone = snapshotZone ?? ShippingZone.Zone3_Remote;
        }

        var order = await _orderService.CreateOrderAsync(model, sessionId, userId);
        
        // Clear shipping snapshot after order is placed
        ClearShippingSnapshot();
        
        return RedirectToAction(nameof(Confirmation), new { orderNumber = order.OrderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var sessionId = GetSessionId();
        ViewBag.CartCount = await _cartService.GetCartCountAsync(sessionId);

        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null) return NotFound();

        ViewBag.Order = order;
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
    
    private int? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;
            
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
            
        return null;
    }
    
    /// <summary>
    /// Saves shipping info to session as snapshot (Requirements 6.1)
    /// </summary>
    private void SaveShippingSnapshot(ShippingInfo shippingInfo, string? district)
    {
        HttpContext.Session.SetString(ShippingFeeSnapshotKey, shippingInfo.ShippingFee.ToString());
        HttpContext.Session.SetString(ShippingZoneSnapshotKey, ((int)shippingInfo.Zone).ToString());
        HttpContext.Session.SetString(ShippingSnapshotTimeKey, DateTime.UtcNow.ToString("O"));
        HttpContext.Session.SetString(ShippingDistrictSnapshotKey, district ?? string.Empty);
    }
    
    /// <summary>
    /// Gets shipping fee from session snapshot (Requirements 6.2, 6.3)
    /// </summary>
    private decimal? GetShippingFeeFromSnapshot()
    {
        var feeStr = HttpContext.Session.GetString(ShippingFeeSnapshotKey);
        if (decimal.TryParse(feeStr, out var fee))
            return fee;
        return null;
    }
    
    /// <summary>
    /// Gets shipping zone from session snapshot
    /// </summary>
    private ShippingZone? GetShippingZoneFromSnapshot()
    {
        var zoneStr = HttpContext.Session.GetString(ShippingZoneSnapshotKey);
        if (int.TryParse(zoneStr, out var zone) && Enum.IsDefined(typeof(ShippingZone), zone))
            return (ShippingZone)zone;
        return null;
    }
    
    /// <summary>
    /// Clears shipping snapshot from session
    /// </summary>
    private void ClearShippingSnapshot()
    {
        HttpContext.Session.Remove(ShippingFeeSnapshotKey);
        HttpContext.Session.Remove(ShippingZoneSnapshotKey);
        HttpContext.Session.Remove(ShippingSnapshotTimeKey);
        HttpContext.Session.Remove(ShippingDistrictSnapshotKey);
    }
}
