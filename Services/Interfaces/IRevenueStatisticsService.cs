using Fruitables.ViewModels;

namespace Fruitables.Services.Interfaces;

/// <summary>
/// Interface cho Revenue Statistics Service - cung cấp thống kê doanh thu chi tiết
/// </summary>
public interface IRevenueStatisticsService
{
    /// <summary>
    /// Lấy tổng quan doanh thu
    /// </summary>
    Task<RevenueOverviewViewModel> GetRevenueOverviewAsync();

    /// <summary>
    /// Lấy doanh thu theo khoảng thời gian
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu</param>
    /// <param name="endDate">Ngày kết thúc</param>
    /// <returns>Result chứa dữ liệu doanh thu hoặc lỗi validation</returns>
    Task<RevenueByDateRangeResult> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Lấy doanh thu phân theo danh mục
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    Task<RevenueByCategoryViewModel> GetRevenueByCategoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Lấy danh sách sản phẩm bán chạy nhất
    /// </summary>
    /// <param name="topCount">Số lượng sản phẩm cần lấy</param>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    /// <param name="categoryId">Lọc theo danh mục (optional)</param>
    Task<TopProductsViewModel> GetTopProductsAsync(
        int topCount = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? categoryId = null);

    /// <summary>
    /// Lấy xu hướng doanh thu theo thời gian
    /// </summary>
    /// <param name="period">Chu kỳ (Daily/Weekly/Monthly)</param>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    Task<RevenueTrendViewModel> GetRevenueTrendAsync(
        TrendPeriod period,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// So sánh doanh thu giữa hai kỳ
    /// </summary>
    /// <param name="currentStart">Ngày bắt đầu kỳ hiện tại</param>
    /// <param name="currentEnd">Ngày kết thúc kỳ hiện tại</param>
    /// <param name="previousStart">Ngày bắt đầu kỳ trước</param>
    /// <param name="previousEnd">Ngày kết thúc kỳ trước</param>
    Task<PeriodComparisonViewModel> ComparePeriodsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd);
}
