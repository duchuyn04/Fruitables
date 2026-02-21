using Fruitables.Models;
using Fruitables.ViewModels;

namespace Fruitables.Repositories.Interfaces;

/// <summary>
/// Repository interface cho Review entity
/// </summary>
public interface IReviewRepository : IRepository<Review>
{
    /// <summary>
    /// Lấy reviews của sản phẩm với pagination và filtering
    /// </summary>
    Task<PagedResult<Review>> GetProductReviewsAsync(ReviewFilterDto filter);
    
    /// <summary>
    /// Lấy review theo ID với includes
    /// </summary>
    Task<Review?> GetReviewWithDetailsAsync(int reviewId);
    
    /// <summary>
    /// Lấy review của user cho sản phẩm cụ thể
    /// </summary>
    Task<Review?> GetUserReviewForProductAsync(int userId, int productId);
    
    /// <summary>
    /// Lấy thống kê reviews của sản phẩm
    /// </summary>
    Task<ReviewStatistics> GetProductReviewStatisticsAsync(int productId);
    
    /// <summary>
    /// Lấy tất cả reviews cho admin với pagination và filtering
    /// </summary>
    Task<PagedResult<Review>> GetAllReviewsForAdminAsync(ReviewAdminFilterDto filter);
    
    /// <summary>
    /// Kiểm tra user đã review sản phẩm chưa
    /// </summary>
    Task<bool> HasUserReviewedProductAsync(int userId, int productId);
    
    /// <summary>
    /// Đếm số reviews của user trong khoảng thời gian
    /// </summary>
    Task<int> CountUserReviewsInPeriodAsync(int userId, DateTime fromDate);
    
    /// <summary>
    /// Lấy reviews cần kiểm duyệt (pending)
    /// </summary>
    Task<List<Review>> GetPendingReviewsAsync();
    
    /// <summary>
    /// Lấy reviews bị báo cáo nhiều
    /// </summary>
    Task<List<Review>> GetReportedReviewsAsync(int minReportCount = 1);
}
