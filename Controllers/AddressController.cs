using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Models;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

[Authorize]
public class AddressController : Controller
{
    private readonly IAddressService _addressService;
    private readonly IProfileService _profileService;
    private readonly IVietnamAddressService _vietnamAddressService;

    public AddressController(
        IAddressService addressService, 
        IProfileService profileService,
        IVietnamAddressService vietnamAddressService)
    {
        _addressService = addressService;
        _profileService = profileService;
        _vietnamAddressService = vietnamAddressService;
    }

    // GET: Address/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var addresses = await _addressService.GetUserAddressesAsync(userId.Value);
        return View(addresses);
    }

    // GET: Address/Create
    public async Task<IActionResult> Create()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Pre-fill from user profile if available
        var address = new Address();
        var profileResult = await _profileService.GetProfileAsync(userId.Value);
        if (profileResult.Success && profileResult.Profile != null)
        {
            address.FullName = profileResult.Profile.Name ?? string.Empty;
            address.Phone = profileResult.Profile.Phone ?? string.Empty;
        }

        return View(address);
    }

    // POST: Address/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Address address)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(address);
        }

        try
        {
            // Sanitize StreetAddress to prevent XSS (Requirements 4.3)
            address.StreetAddress = _vietnamAddressService.SanitizeStreetAddress(address.StreetAddress);
            
            var setAsDefault = address.IsDefault;
            address.UserId = userId.Value;
            address.IsDefault = false; // Let service handle first address default
            
            // Address model stores all code and name fields (Requirements 5.2)
            var created = await _addressService.CreateAddressAsync(address);
            
            // If user checked "set as default", call SetDefaultAddressAsync
            if (setAsDefault)
            {
                await _addressService.SetDefaultAddressAsync(userId.Value, created.Id);
            }
            
            TempData["SuccessMessage"] = "Địa chỉ đã được thêm thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(address);
        }
    }

    // GET: Address/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var address = await _addressService.GetAddressByIdAsync(id);
        if (address == null || address.UserId != userId.Value)
        {
            return NotFound();
        }

        return View(address);
    }

    // POST: Address/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Address address)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != address.Id)
        {
            return BadRequest();
        }

        // Verify ownership
        var existing = await _addressService.GetAddressByIdAsync(id);
        if (existing == null || existing.UserId != userId.Value)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(address);
        }

        try
        {
            // Sanitize StreetAddress to prevent XSS (Requirements 4.3)
            address.StreetAddress = _vietnamAddressService.SanitizeStreetAddress(address.StreetAddress);
            
            var setAsDefault = address.IsDefault;
            address.UserId = userId.Value;
            address.IsDefault = existing.IsDefault; // Keep current default status for update
            
            // Address model stores all code and name fields (Requirements 5.2, 8.1)
            await _addressService.UpdateAddressAsync(address);
            
            // If user checked "set as default" and it wasn't already default
            if (setAsDefault && !existing.IsDefault)
            {
                await _addressService.SetDefaultAddressAsync(userId.Value, id);
            }
            
            TempData["SuccessMessage"] = "Địa chỉ đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(address);
        }
    }

    // POST: Address/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var address = await _addressService.GetAddressByIdAsync(id);
        if (address == null || address.UserId != userId.Value)
        {
            return NotFound();
        }

        var result = await _addressService.DeleteAddressAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Địa chỉ đã được xóa thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xóa địa chỉ.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Address/SetDefault/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Unauthorized" });
            return RedirectToAction("Login", "Account");
        }

        var address = await _addressService.GetAddressByIdAsync(id);
        if (address == null || address.UserId != userId.Value)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
            return NotFound();
        }

        var result = await _addressService.SetDefaultAddressAsync(userId.Value, id);
        
        // Return JSON for AJAX requests
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { 
                success = result, 
                message = result ? "Địa chỉ mặc định đã được cập nhật." : "Không thể đặt địa chỉ mặc định."
            });
        }
        
        if (result)
        {
            TempData["SuccessMessage"] = "Địa chỉ mặc định đã được cập nhật.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể đặt địa chỉ mặc định.";
        }

        return RedirectToAction(nameof(Index));
    }
    
    // POST: Address/DeleteAjax/5 - AJAX endpoint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var address = await _addressService.GetAddressByIdAsync(id);
        if (address == null || address.UserId != userId.Value)
        {
            return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
        }

        var result = await _addressService.DeleteAddressAsync(id);
        
        // Get new default address ID if any
        int? newDefaultId = null;
        if (result)
        {
            var defaultAddress = await _addressService.GetDefaultAddressAsync(userId.Value);
            newDefaultId = defaultAddress?.Id;
        }
        
        return Json(new { 
            success = result, 
            message = result ? "Địa chỉ đã được xóa thành công." : "Không thể xóa địa chỉ.",
            newDefaultId = newDefaultId
        });
    }

    /// <summary>
    /// Lấy userId từ claims
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
