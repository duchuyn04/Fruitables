using Fruitables.Models;

namespace Fruitables.ViewModels;

/// <summary>
/// Filter DTO for customer review listing
/// </summary>
public class ReviewFilterDto
{
    public int ProductId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ReviewSortBy SortBy { get; set; } = ReviewSortBy.Newest;
    public int? RatingFilter { get; set; } // null = all, 1-5 = specific rating
}

/// <summary>
/// Sort options for reviews
/// </summary>
public enum ReviewSortBy
{
    Newest,
    Oldest,
    HighestRating,
    LowestRating,
    MostHelpful
}

/// <summary>
/// Filter DTO for admin review management
/// </summary>
public class ReviewAdminFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Filters
    public int? ProductId { get; set; }
    public int? UserId { get; set; }
    public ReviewStatus? Status { get; set; }
    public int? Rating { get; set; }
    public bool? IsHidden { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsVerifiedPurchase { get; set; }
    public bool? HasReports { get; set; }
    
    // Search
    public string? SearchTerm { get; set; } // Search in comment, user name, product name
    
    // Date range
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Sort
    public ReviewAdminSortBy SortBy { get; set; } = ReviewAdminSortBy.Newest;
}

/// <summary>
/// Sort options for admin review management
/// </summary>
public enum ReviewAdminSortBy
{
    Newest,
    Oldest,
    HighestRating,
    LowestRating,
    MostReported,
    MostHelpful
}

/// <summary>
/// Filter DTO for review reports
/// </summary>
public class ReportFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Filters
    public ReportStatus? Status { get; set; }
    public ReportReason? Reason { get; set; }
    public int? ProductId { get; set; }
    public int? ReviewId { get; set; }
    
    // Date range
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Sort
    public ReportSortBy SortBy { get; set; } = ReportSortBy.Newest;
}

/// <summary>
/// Sort options for reports
/// </summary>
public enum ReportSortBy
{
    Newest,
    Oldest
}

/// <summary>
/// Action to take on a report
/// </summary>
public enum ReportAction
{
    Resolve,    // Mark as resolved (hide the review)
    Dismiss     // Dismiss the report (keep the review visible)
}
