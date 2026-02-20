using System.ComponentModel.DataAnnotations;
using Fruitables.Models;

namespace Fruitables.ViewModels;

// Customer ViewModels

/// <summary>
/// ViewModel for displaying a review to customers
/// </summary>
public class ReviewViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // For current user
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    public bool HasReported { get; set; }
}

/// <summary>
/// ViewModel for admin review management
/// </summary>
public class ReviewAdminViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    
    public ReviewStatus Status { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public string? HiddenByAdminName { get; set; }
    public DateTime? HiddenAt { get; set; }
    
    public bool IsDeleted { get; set; }
    public string? DeletedByAdminName { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulCount { get; set; }
    public int ReportCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Statistics for product reviews
/// </summary>
public class ReviewStatistics
{
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public Dictionary<int, decimal> RatingPercentages { get; set; } = new();
}

/// <summary>
/// Admin statistics for review management
/// </summary>
public class ReviewAdminStatistics
{
    public int TotalReviews { get; set; }
    public int PendingReviews { get; set; }
    public int ApprovedReviews { get; set; }
    public int RejectedReviews { get; set; }
    public int HiddenReviews { get; set; }
    public int DeletedReviews { get; set; }
    public int ReportedReviews { get; set; }
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    
    public decimal AverageRating { get; set; }
    public int ReviewsToday { get; set; }
    public int ReviewsThisWeek { get; set; }
    public int ReviewsThisMonth { get; set; }
    
    public List<TopReviewedProduct> TopReviewedProducts { get; set; } = new();
    public List<TopRatedProduct> TopRatedProducts { get; set; } = new();
}

public class TopReviewedProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ReviewCount { get; set; }
}

public class TopRatedProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

// DTOs for API requests

/// <summary>
/// DTO for creating a new review
/// </summary>
public class CreateReviewDto
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5 sao")]
    public int Rating { get; set; }
    
    [MaxLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự")]
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for updating an existing review
/// </summary>
public class UpdateReviewDto
{
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5 sao")]
    public int? Rating { get; set; }
    
    [MaxLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự")]
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for reporting a review
/// </summary>
public class ReportReviewDto
{
    [Required]
    public ReportReason Reason { get; set; }
    
    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }
}

/// <summary>
/// Result of a review operation
/// </summary>
public class ReviewResult
{
    public bool Success { get; set; }
    public ReviewErrorCode? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public ReviewViewModel? Data { get; set; }
}

/// <summary>
/// Error codes for review operations
/// </summary>
public enum ReviewErrorCode
{
    AlreadyReviewed,
    NotPurchased,
    NotFound,
    Unauthorized,
    RateLimitExceeded,
    InvalidRating,
    CommentTooLong,
    EditTimeExpired,
    AlreadyReported
}

/// <summary>
/// ViewModel for review report
/// </summary>
public class ReviewReportViewModel
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string ReviewComment { get; set; } = string.Empty;
    public int ReviewRating { get; set; }
    public string ReviewUserName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    
    public int ReportedByUserId { get; set; }
    public string ReportedByUserName { get; set; } = string.Empty;
    public ReportReason Reason { get; set; }
    public string? Description { get; set; }
    
    public ReportStatus Status { get; set; }
    public string? HandledByAdminName { get; set; }
    public DateTime? HandledAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
