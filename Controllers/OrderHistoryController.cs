using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Fruitables.Models;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Controllers;

/// <summary>
/// Controller quản lý lịch sử đơn hàng của khách hàng
/// </summary>
[Authorize]
public class OrderHistoryController : Controller
{
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly IMemoryCache _cache;
    private const string OrderStatusesCacheKey = "OrderHistory_Statuses";
    private const int MaxPageSize = 50;
    private const int MaxSearchTermLength = 50;

    public OrderHistoryController(IOrderHistoryService orderHistoryService, IMemoryCache cache)
    {
        _orderHistoryService = orderHistoryService;
        _cache = cache;
    }

    /// <summary>
    /// GET: /OrderHistory - Danh sách đơn hàng với phân trang và lọc
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(OrderHistoryFilterViewModel filter)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate và sanitize input parameters
        filter = SanitizeFilter(filter);

        var result = await _orderHistoryService.GetOrderHistoryAsync(userId.Value, filter);

        ViewBag.Filter = filter;
        ViewBag.Statuses = GetOrderStatusesCached();

        return View(result);
    }

    /// <summary>
    /// GET: /OrderHistory/Filter - AJAX endpoint để lọc đơn hàng real-time
    /// Trả về PartialView chứa danh sách đơn hàng và phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Filter(OrderHistoryFilterViewModel filter)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            // Trả về 401 cho AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Unauthorized();
            }
            return RedirectToAction("Login", "Account");
        }

        // Validate và sanitize input parameters
        filter = SanitizeFilter(filter);

        try
        {
            var result = await _orderHistoryService.GetOrderHistoryAsync(userId.Value, filter);

            ViewBag.Filter = filter;

            // Trả về PartialView cho AJAX request
            return PartialView("_OrderListContainer", result);
        }
        catch (Exception)
        {
            // Log exception nếu cần
            // Trả về error partial view
            return PartialView("_OrderListError", "Có lỗi xảy ra khi tải danh sách đơn hàng. Vui lòng thử lại.");
        }
    }

    /// <summary>
    /// GET: /OrderHistory/Details/{id} - Chi tiết đơn hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var orderDetail = await _orderHistoryService.GetOrderDetailAsync(id, userId.Value);
        if (orderDetail == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.";
            return RedirectToAction(nameof(Index));
        }

        return View(orderDetail);
    }

    /// <summary>
    /// POST: /OrderHistory/Cancel/{id} - Hủy đơn hàng
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string cancelReason)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate order id
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Mã đơn hàng không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        // Validate và sanitize cancel reason
        var sanitizedReason = SanitizeCancelReason(cancelReason);
        if (string.IsNullOrWhiteSpace(sanitizedReason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng (tối thiểu 5 ký tự).";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _orderHistoryService.CancelOrderAsync(id, userId.Value, sanitizedReason);
        if (!result)
        {
            TempData["ErrorMessage"] = "Không thể hủy đơn hàng. Đơn hàng có thể đã được xử lý hoặc bạn không có quyền hủy.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Sanitize cancel reason để tránh XSS và injection
    /// </summary>
    private static string? SanitizeCancelReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        // Trim và giới hạn độ dài (max 500 ký tự)
        var sanitized = reason.Trim();
        if (sanitized.Length > 500)
        {
            sanitized = sanitized[..500];
        }

        // Yêu cầu tối thiểu 5 ký tự
        if (sanitized.Length < 5)
        {
            return null;
        }

        // Loại bỏ các ký tự HTML nguy hiểm
        sanitized = sanitized
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");

        return sanitized;
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

    /// <summary>
    /// Lấy danh sách trạng thái đơn hàng cho dropdown filter (với caching)
    /// </summary>
    private List<SelectListItem> GetOrderStatusesCached()
    {
        return _cache.GetOrCreate(OrderStatusesCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            entry.Priority = CacheItemPriority.Low;
            return GetOrderStatuses();
        })!;
    }

    /// <summary>
    /// Lấy danh sách trạng thái đơn hàng cho dropdown filter
    /// </summary>
    private static List<SelectListItem> GetOrderStatuses()
    {
        return new List<SelectListItem>
        {
            new() { Value = "", Text = "Tất cả trạng thái" },
            new() { Value = ((int)OrderStatus.Pending).ToString(), Text = "Chờ xử lý" },
            new() { Value = ((int)OrderStatus.Processing).ToString(), Text = "Đang xử lý" },
            new() { Value = ((int)OrderStatus.Shipped).ToString(), Text = "Đang giao" },
            new() { Value = ((int)OrderStatus.Delivered).ToString(), Text = "Đã giao" },
            new() { Value = ((int)OrderStatus.Cancelled).ToString(), Text = "Đã hủy" }
        };
    }

    /// <summary>
    /// Validate và sanitize filter parameters để tránh injection attacks
    /// </summary>
    private static OrderHistoryFilterViewModel SanitizeFilter(OrderHistoryFilterViewModel filter)
    {
        // Validate page và pageSize
        filter.Page = filter.Page < 1 ? 1 : filter.Page;
        filter.PageSize = filter.PageSize < 1 ? 10 : Math.Min(filter.PageSize, MaxPageSize);

        // Sanitize search term - chỉ cho phép alphanumeric và dấu gạch ngang
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            // Giới hạn độ dài
            var searchTerm = filter.SearchTerm.Trim();
            if (searchTerm.Length > MaxSearchTermLength)
            {
                searchTerm = searchTerm[..MaxSearchTermLength];
            }

            // Chỉ cho phép ký tự an toàn (alphanumeric, dấu gạch ngang, dấu cách)
            filter.SearchTerm = Regex.Replace(searchTerm, @"[^\w\s\-]", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        // Validate status - đảm bảo là giá trị enum hợp lệ
        if (filter.Status.HasValue && !Enum.IsDefined(typeof(OrderStatus), filter.Status.Value))
        {
            filter.Status = null;
        }

        // Validate date range
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
        {
            // Swap dates nếu FromDate > ToDate
            (filter.FromDate, filter.ToDate) = (filter.ToDate, filter.FromDate);
        }

        return filter;
    }
}

/// <summary>
/// Helper class cho dropdown
/// </summary>
public class SelectListItem
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
