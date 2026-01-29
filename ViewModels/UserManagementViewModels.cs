using Fruitables.Models;

namespace Fruitables.ViewModels;

#region Result Types

/// <summary>
/// Result type cho các operations trong User Management
/// </summary>
public class UserManagementResult<T>
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public T? Data { get; set; }

    public static UserManagementResult<T> Success(T data) => new()
    {
        IsValid = true,
        Data = data
    };

    public static UserManagementResult<T> ValidationError(string message, string? code = null) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ErrorCode = code ?? "ValidationError"
    };

    public static UserManagementResult<T> NotFound(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ErrorCode = "NotFound"
    };

    public static UserManagementResult<T> Unauthorized(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ErrorCode = "Unauthorized"
    };

    public static UserManagementResult<T> ConcurrencyError(string message, string adminName, DateTime lockedAt) => new()
    {
        IsValid = false,
        ErrorMessage = $"User đã bị khóa bởi {adminName} lúc {lockedAt:dd/MM/yyyy HH:mm}",
        ErrorCode = "ConcurrencyError"
    };
}

#endregion

#region Filter and List

/// <summary>
/// Request lọc danh sách khách hàng
/// </summary>
public class UserFilterRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


/// <summary>
/// Kết quả danh sách khách hàng với phân trang
/// </summary>
public class UserListResult
{
    public List<CustomerListItemViewModel> Customers { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

/// <summary>
/// Item trong danh sách khách hàng
/// </summary>
public class CustomerListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public bool IsVip { get; set; }
}

#endregion

#region Customer Detail

/// <summary>
/// Chi tiết khách hàng
/// </summary>
public class CustomerDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsGoogleAccount { get; set; }
    public bool IsVip { get; set; }

    public CustomerStatisticsViewModel Statistics { get; set; } = new();
    public List<AddressViewModel> Addresses { get; set; } = new();
    public AccountLockInfoViewModel? LockInfo { get; set; }
}

/// <summary>
/// Thống kê khách hàng
/// </summary>
public class CustomerStatisticsViewModel
{
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
}

/// <summary>
/// Thông tin khóa tài khoản
/// </summary>
public class AccountLockInfoViewModel
{
    public LockType LockType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string LockedByAdminName { get; set; } = string.Empty;
    public int LockedByAdminId { get; set; }
}

#endregion

#region Purchase History

/// <summary>
/// Lịch sử mua hàng của khách hàng
/// </summary>
public class CustomerPurchaseHistoryViewModel
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<UserOrderSummaryViewModel> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

/// <summary>
/// Tóm tắt đơn hàng cho User Management
/// </summary>
public class UserOrderSummaryViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

#endregion


#region Lock Account

/// <summary>
/// Request khóa tài khoản
/// </summary>
public class LockAccountRequest
{
    public int CustomerId { get; set; }
    public int AdminId { get; set; }
    public string AdminRole { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public LockType LockType { get; set; }
    public int? LockDurationDays { get; set; }
    public bool ConfirmLockWithPendingOrders { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Kết quả khóa tài khoản
/// </summary>
public class LockAccountResultViewModel
{
    public bool Success { get; set; }
    public bool HasPendingOrders { get; set; }
    public int PendingOrderCount { get; set; }
    public List<string> PendingOrderNumbers { get; set; } = new();
    public bool RequiresConfirmation { get; set; }
    public bool IsVipCustomer { get; set; }
}

#endregion

#region Unlock Account

/// <summary>
/// Request mở khóa tài khoản
/// </summary>
public class UnlockAccountRequest
{
    public int CustomerId { get; set; }
    public int AdminId { get; set; }
    public string AdminRole { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

#endregion

#region Account Log

/// <summary>
/// Log khóa/mở khóa tài khoản
/// </summary>
public class UserAccountLogViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public LockType? LockType { get; set; }
    public string? ViolationType { get; set; }
    public string? Reason { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public int AdminId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

#endregion
