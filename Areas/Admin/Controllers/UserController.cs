using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using Fruitables.Models;
using Fruitables.Attributes;
using System.Security.Claims;

namespace Fruitables.Areas.Admin.Controllers;

/// <summary>
/// Controller for User Management in Admin Panel
/// Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 7.2, 10.5, 10.6
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UserController : Controller
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRbacService _rbacService;

    public UserController(
        IUserManagementService userManagementService,
        IRbacService rbacService)
    {
        _userManagementService = userManagementService;
        _rbacService = rbacService;
    }

    /// <summary>
    /// Display list of customers with filtering and pagination
    /// GET: /Admin/User
    /// Requirements: 1.1 - Display customer list (Admin + SuperAdmin)
    /// </summary>
    public async Task<IActionResult> Index(
        string? searchTerm,
        bool? isActive,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        int page = 1)
    {
        var filter = new UserFilterRequest
        {
            SearchTerm = searchTerm,
            IsActive = isActive,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = 20
        };

        var result = await _userManagementService.GetCustomersAsync(filter);

        // Store filter values for view
        ViewBag.SearchTerm = searchTerm;
        ViewBag.IsActive = isActive;
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        
        // Check if current user is SuperAdmin for lock/unlock permissions
        ViewBag.CanLockAccount = _userManagementService.CanLockAccount(GetCurrentUserRole());

        return View(result);
    }

    /// <summary>
    /// Display detailed customer information
    /// GET: /Admin/User/Detail/{id}
    /// Requirements: 2.1 - Display customer details (Admin + SuperAdmin)
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        var result = await _userManagementService.GetCustomerDetailAsync(id);

        if (!result.IsValid)
        {
            TempData["Error"] = result.ErrorMessage ?? "Không tìm thấy khách hàng";
            return RedirectToAction(nameof(Index));
        }

        // Check if current user is SuperAdmin for lock/unlock permissions
        var currentUserRole = GetCurrentUserRole();
        ViewBag.CanLockAccount = _userManagementService.CanLockAccount(currentUserRole);
        ViewBag.CurrentUserRole = currentUserRole;
        ViewBag.CurrentAdminId = GetCurrentAdminId();

        return View(result.Data);
    }

    /// <summary>
    /// API to get customer purchase history
    /// GET: /Admin/User/PurchaseHistory
    /// Requirements: 3.1 - Get purchase history (Admin + SuperAdmin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PurchaseHistory(
        int customerId,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10)
    {
        var result = await _userManagementService.GetPurchaseHistoryAsync(
            customerId,
            status,
            startDate,
            endDate,
            page,
            pageSize);

        if (!result.IsValid)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Json(result.Data);
    }

    /// <summary>
    /// API to lock customer account
    /// POST: /Admin/User/Lock
    /// Requirements: 4.1 - Lock account (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock([FromBody] LockAccountRequest request)
    {
        // Set admin info and capture IP/UserAgent
        request.AdminId = GetCurrentAdminId();
        request.AdminRole = GetCurrentUserRole();
        request.IpAddress = GetClientIpAddress();
        request.UserAgent = Request.Headers["User-Agent"].ToString();

        var result = await _userManagementService.LockAccountAsync(request);

        if (!result.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = result.ErrorMessage,
                errorCode = result.ErrorCode
            });
        }

        return Json(new
        {
            success = true,
            data = result.Data
        });
    }

    /// <summary>
    /// API to unlock customer account
    /// POST: /Admin/User/Unlock
    /// Requirements: 5.1 - Unlock account (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock([FromBody] UnlockAccountRequest request)
    {
        // Set admin info and capture IP/UserAgent
        request.AdminId = GetCurrentAdminId();
        request.AdminRole = GetCurrentUserRole();
        request.IpAddress = GetClientIpAddress();
        request.UserAgent = Request.Headers["User-Agent"].ToString();

        var result = await _userManagementService.UnlockAccountAsync(request);

        if (!result.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = result.ErrorMessage,
                errorCode = result.ErrorCode
            });
        }

        return Json(new
        {
            success = true,
            message = "Mở khóa tài khoản thành công"
        });
    }

    /// <summary>
    /// API to get account lock/unlock history
    /// GET: /Admin/User/AccountLogs
    /// Requirements: 6.1 - Get account logs (Admin + SuperAdmin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AccountLogs(int customerId)
    {
        var result = await _userManagementService.GetAccountLogsAsync(customerId);

        if (!result.IsValid)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Json(result.Data);
    }

    /// <summary>
    /// Display role management page for a user
    /// GET: /Admin/User/ManageRoles/{id}
    /// Requirements: 10.5 - Manage user roles
    /// </summary>
    [RequirePermission("users.update")]
    public async Task<IActionResult> ManageRoles(int id)
    {
        // Get user details
        var userResult = await _userManagementService.GetCustomerDetailAsync(id);
        if (!userResult.IsValid)
        {
            TempData["Error"] = userResult.ErrorMessage ?? "Không tìm thấy người dùng";
            return RedirectToAction(nameof(Index));
        }

        // Get user's current roles
        var userRoles = await _rbacService.GetUserRolesAsync(id);
        
        // Get all available roles
        var allRoles = await _rbacService.GetAllRolesAsync(includeInactive: false);
        
        // Get user's effective permissions
        var effectivePermissions = await _rbacService.GetUserPermissionsAsync(id);
        
        // Get permissions grouped by module for display
        var permissionsGrouped = await _rbacService.GetPermissionsGroupedByModuleAsync();

        var viewModel = new UserRoleManagementViewModel
        {
            UserId = id,
            UserName = userResult.Data!.Name,
            UserEmail = userResult.Data.Email,
            CurrentRoles = userRoles,
            AvailableRoles = allRoles,
            EffectivePermissions = effectivePermissions,
            PermissionsGroupedByModule = permissionsGrouped
        };

        return View(viewModel);
    }

    /// <summary>
    /// API to assign a role to a user
    /// POST: /Admin/User/AssignRole
    /// Requirements: 10.5 - Assign role to user
    /// </summary>
    [HttpPost]
    [RequirePermission("users.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            var adminId = GetCurrentAdminId();
            await _rbacService.AssignRoleToUserAsync(request.UserId, request.RoleId, adminId);

            return Json(new
            {
                success = true,
                message = "Gán vai trò thành công"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// API to revoke a role from a user
    /// POST: /Admin/User/RevokeRole
    /// Requirements: 10.5 - Revoke role from user
    /// </summary>
    [HttpPost]
    [RequirePermission("users.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRole([FromBody] RevokeRoleRequest request)
    {
        try
        {
            var adminId = GetCurrentAdminId();
            await _rbacService.RevokeRoleFromUserAsync(request.UserId, request.RoleId, adminId);

            return Json(new
            {
                success = true,
                message = "Thu hồi vai trò thành công"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// API to get user's effective permissions
    /// GET: /Admin/User/GetEffectivePermissions
    /// Requirements: 10.6 - Display effective permissions
    /// </summary>
    [HttpGet]
    [RequirePermission("users.update")]
    public async Task<IActionResult> GetEffectivePermissions(int userId)
    {
        try
        {
            var permissions = await _rbacService.GetUserPermissionsAsync(userId);
            var permissionsGrouped = await _rbacService.GetPermissionsGroupedByModuleAsync();
            
            // Group effective permissions by module
            var effectiveByModule = new Dictionary<string, List<string>>();
            foreach (var permission in permissions)
            {
                var parts = permission.Split('.');
                if (parts.Length == 2)
                {
                    var module = parts[0];
                    if (!effectiveByModule.ContainsKey(module))
                    {
                        effectiveByModule[module] = new List<string>();
                    }
                    effectiveByModule[module].Add(permission);
                }
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    permissions = permissions,
                    groupedByModule = effectiveByModule
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Get current admin user ID from claims
    /// </summary>
    private int GetCurrentAdminId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    /// <summary>
    /// Get current user role from claims
    /// </summary>
    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
    }

    /// <summary>
    /// Get client IP address from request
    /// Requirements: 6.3 - Log IP address
    /// </summary>
    private string? GetClientIpAddress()
    {
        // Check for forwarded IP first (in case behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP address
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
