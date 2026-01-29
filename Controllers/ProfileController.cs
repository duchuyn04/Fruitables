using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Controllers;

/// <summary>
/// Controller quản lý thông tin profile người dùng
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// GET: /Profile - Xem thông tin profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _profileService.GetProfileAsync(userId.Value);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        return View(result.Profile);
    }

    /// <summary>
    /// GET: /Profile/Edit - Form chỉnh sửa profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _profileService.GetProfileAsync(userId.Value);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var model = new UpdateProfileRequest
        {
            UserId = result.Profile!.Id,
            Name = result.Profile.Name,
            Phone = result.Profile.Phone
        };

        ViewBag.Profile = result.Profile;
        return View(model);
    }

    /// <summary>
    /// POST: /Profile/Edit - Lưu thông tin profile
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileRequest model)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        model.UserId = userId.Value;

        if (!ModelState.IsValid)
        {
            var profileResult = await _profileService.GetProfileAsync(userId.Value);
            ViewBag.Profile = profileResult.Profile;
            return View(model);
        }

        var result = await _profileService.UpdateProfileAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Cập nhật thất bại");
            var profileResult = await _profileService.GetProfileAsync(userId.Value);
            ViewBag.Profile = profileResult.Profile;
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Profile/UploadAvatar - Upload avatar mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (avatar == null || avatar.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file ảnh";
            return RedirectToAction(nameof(Edit));
        }

        var result = await _profileService.UpdateAvatarAsync(userId.Value, avatar);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Edit));
        }

        TempData["SuccessMessage"] = "Cập nhật avatar thành công!";
        return RedirectToAction(nameof(Edit));
    }

    /// <summary>
    /// POST: /Profile/DeleteAvatar - Xóa avatar
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _profileService.DeleteAvatarAsync(userId.Value);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Edit));
        }

        TempData["SuccessMessage"] = "Đã xóa avatar!";
        return RedirectToAction(nameof(Edit));
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
