using Fruitables.ViewModels;

namespace Fruitables.Services.Interfaces;

/// <summary>
/// Interface cho Cancelled Orders Statistics Service - cung cấp thống kê đơn hàng bị hủy
/// </summary>
public interface ICancelledOrdersStatisticsService
{
    /// <summary>
    /// Lấy tổng quan đơn hủy trong khoảng thời gian
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    /// <returns>Result chứa dữ liệu tổng quan hoặc lỗi validation</returns>
    Task<CancelledOrdersResult<CancelledOrdersOverviewViewModel>> GetOverviewAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null);

    /// <summary>
    /// Lấy xu hướng đơn hủy theo thời gian
    /// </summary>
    /// <param name="period">Chu kỳ (Daily/Weekly/Monthly)</param>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    /// <returns>Result chứa dữ liệu xu hướng hoặc lỗi validation</returns>
    Task<CancelledOrdersResult<CancelledOrdersTrendViewModel>> GetTrendAsync(
        TrendPeriod period, 
        DateTime? startDate = null, 
        DateTime? endDate = null);

    /// <summary>
    /// Lấy thống kê theo lý do hủy
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu (optional)</param>
    /// <param name="endDate">Ngày kết thúc (optional)</param>
    /// <returns>Result chứa dữ liệu thống kê lý do hoặc lỗi validation</returns>
    Task<CancelledOrdersResult<CancelReasonStatisticsViewModel>> GetReasonStatisticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null);

    /// <summary>
    /// So sánh đơn hủy giữa hai kỳ
    /// </summary>
    /// <param name="currentStart">Ngày bắt đầu kỳ hiện tại</param>
    /// <param name="currentEnd">Ngày kết thúc kỳ hiện tại</param>
    /// <param name="previousStart">Ngày bắt đầu kỳ trước</param>
    /// <param name="previousEnd">Ngày kết thúc kỳ trước</param>
    /// <returns>Result chứa dữ liệu so sánh giữa hai kỳ hoặc lỗi validation</returns>
    Task<CancelledOrdersResult<CancelledOrdersComparisonViewModel>> ComparePeriodsAsync(
        DateTime currentStart, 
        DateTime currentEnd, 
        DateTime previousStart, 
        DateTime previousEnd);
}
