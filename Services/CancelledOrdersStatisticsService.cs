using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Services;

/// <summary>
/// Service xử lý thống kê đơn hàng bị hủy.
/// Đơn hủy = Order có OrderStatus = Cancelled
/// </summary>
public class CancelledOrdersStatisticsService : ICancelledOrdersStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelledOrdersStatisticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CancelledOrdersResult<CancelledOrdersOverviewViewModel>> GetOverviewAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Validate date range if both provided
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            return CancelledOrdersResult<CancelledOrdersOverviewViewModel>.ValidationError(
                $"Ngày bắt đầu ({startDate.Value:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({endDate.Value:dd/MM/yyyy}).");
        }

        // Get all orders and cancelled orders
        var allOrders = await GetAllOrdersAsync();
        var cancelledOrders = await GetCancelledOrdersAsync();

        // Filter by date range if provided
        if (startDate.HasValue && endDate.HasValue)
        {
            allOrders = FilterByDateRange(allOrders, startDate.Value, endDate.Value);
            cancelledOrders = FilterByDateRange(cancelledOrders, startDate.Value, endDate.Value);
        }

        var totalOrders = allOrders.Count;
        var totalCancelled = cancelledOrders.Count;
        var totalCancelledValue = cancelledOrders.Sum(o => o.Total);

        // Calculate cancellation rate (Requirements: 1.2)
        var cancellationRate = totalOrders > 0
            ? Math.Round((decimal)totalCancelled / totalOrders * 100, 2)
            : 0;

        return CancelledOrdersResult<CancelledOrdersOverviewViewModel>.Success(new CancelledOrdersOverviewViewModel
        {
            TotalCancelledOrders = totalCancelled,
            CancellationRate = cancellationRate,
            TotalCancelledValue = totalCancelledValue,
            TotalOrders = totalOrders,
            Filter = new RevenueFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            }
        });
    }


    /// <inheritdoc />
    public async Task<CancelledOrdersResult<CancelledOrdersTrendViewModel>> GetTrendAsync(
        TrendPeriod period,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Validate date range if both provided
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            return CancelledOrdersResult<CancelledOrdersTrendViewModel>.ValidationError(
                $"Ngày bắt đầu ({startDate.Value:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({endDate.Value:dd/MM/yyyy}).");
        }

        var allOrders = await GetAllOrdersAsync();
        var cancelledOrders = await GetCancelledOrdersAsync();

        if (startDate.HasValue && endDate.HasValue)
        {
            allOrders = FilterByDateRange(allOrders, startDate.Value, endDate.Value);
            cancelledOrders = FilterByDateRange(cancelledOrders, startDate.Value, endDate.Value);
        }

        var result = new CancelledOrdersTrendViewModel { Period = period };

        switch (period)
        {
            case TrendPeriod.Daily:
                BuildDailyTrend(result, allOrders, cancelledOrders, startDate, endDate);
                break;
            case TrendPeriod.Weekly:
                BuildWeeklyTrend(result, allOrders, cancelledOrders, startDate, endDate);
                break;
            case TrendPeriod.Monthly:
                BuildMonthlyTrend(result, allOrders, cancelledOrders, startDate, endDate);
                break;
        }

        return CancelledOrdersResult<CancelledOrdersTrendViewModel>.Success(result);
    }

    /// <inheritdoc />
    public async Task<CancelledOrdersResult<CancelReasonStatisticsViewModel>> GetReasonStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Validate date range if both provided
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            return CancelledOrdersResult<CancelReasonStatisticsViewModel>.ValidationError(
                $"Ngày bắt đầu ({startDate.Value:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({endDate.Value:dd/MM/yyyy}).");
        }

        var cancelledOrders = await GetCancelledOrdersAsync();

        if (startDate.HasValue && endDate.HasValue)
        {
            cancelledOrders = FilterByDateRange(cancelledOrders, startDate.Value, endDate.Value);
        }

        var totalCancelled = cancelledOrders.Count;

        // Group by reason, treating null/empty as "Không có lý do" (Requirements: 4.2)
        var reasonGroups = cancelledOrders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.CancelReason) ? "Không có lý do" : o.CancelReason)
            .Select(g => new CancelReasonItem
            {
                Reason = g.Key,
                Count = g.Count(),
                Percentage = totalCancelled > 0
                    ? Math.Round((decimal)g.Count() / totalCancelled * 100, 2)
                    : 0
            })
            .OrderByDescending(r => r.Count) // Requirements: 4.3
            .ToList();

        return CancelledOrdersResult<CancelReasonStatisticsViewModel>.Success(new CancelReasonStatisticsViewModel
        {
            Reasons = reasonGroups,
            TotalCancelledOrders = totalCancelled
        });
    }

    /// <inheritdoc />
    public async Task<CancelledOrdersResult<CancelledOrdersComparisonViewModel>> ComparePeriodsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd)
    {
        // Validate current period date range (Requirements: 3.5)
        if (currentStart > currentEnd)
        {
            return CancelledOrdersResult<CancelledOrdersComparisonViewModel>.ValidationError(
                $"Ngày bắt đầu kỳ hiện tại ({currentStart:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({currentEnd:dd/MM/yyyy}).");
        }

        // Validate previous period date range (Requirements: 3.5)
        if (previousStart > previousEnd)
        {
            return CancelledOrdersResult<CancelledOrdersComparisonViewModel>.ValidationError(
                $"Ngày bắt đầu kỳ trước ({previousStart:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({previousEnd:dd/MM/yyyy}).");
        }

        var cancelledOrders = await GetCancelledOrdersAsync();

        var currentCancelled = FilterByDateRange(cancelledOrders, currentStart, currentEnd);
        var previousCancelled = FilterByDateRange(cancelledOrders, previousStart, previousEnd);

        var currentCount = currentCancelled.Count;
        var previousCount = previousCancelled.Count;
        var changeAmount = currentCount - previousCount;

        // Calculate change percent (Requirements: 5.2, 5.3, 5.4)
        decimal changePercent;
        if (previousCount == 0)
        {
            // Requirements: 5.3 - If previous = 0 and current > 0, show 100%
            // Requirements: 5.4 - If both = 0, show 0%
            changePercent = currentCount > 0 ? 100 : 0;
        }
        else
        {
            changePercent = Math.Round((decimal)changeAmount / previousCount * 100, 2);
        }

        return CancelledOrdersResult<CancelledOrdersComparisonViewModel>.Success(new CancelledOrdersComparisonViewModel
        {
            CurrentPeriodCancelled = currentCount,
            PreviousPeriodCancelled = previousCount,
            ChangePercent = changePercent,
            ChangeAmount = changeAmount,
            CurrentPeriodLabel = $"{currentStart:dd/MM/yyyy} - {currentEnd:dd/MM/yyyy}",
            PreviousPeriodLabel = $"{previousStart:dd/MM/yyyy} - {previousEnd:dd/MM/yyyy}"
        });
    }


    #region Helper Methods

    /// <summary>
    /// Lấy tất cả đơn hàng
    /// </summary>
    private async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tất cả đơn hàng bị hủy (OrderStatus = Cancelled)
    /// </summary>
    private async Task<List<Order>> GetCancelledOrdersAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled)
            .ToListAsync();
    }

    /// <summary>
    /// Lọc đơn hàng theo khoảng thời gian.
    /// Chuyển đổi CreatedAt (UTC) sang giờ Việt Nam trước khi so sánh.
    /// </summary>
    private static List<Order> FilterByDateRange(List<Order> orders, DateTime startDate, DateTime endDate)
    {
        return orders.Where(o =>
        {
            var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
            return vietnamTime >= startDate && vietnamTime <= endDate;
        }).ToList();
    }

    /// <summary>
    /// Chuyển đổi DateTime UTC sang giờ Việt Nam (UTC+7)
    /// </summary>
    private static DateTime ConvertToVietnamTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }
        else if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        try
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, vietnamTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, vietnamTimeZone);
        }
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo ngày
    /// </summary>
    private static void BuildDailyTrend(
        CancelledOrdersTrendViewModel result,
        List<Order> allOrders,
        List<Order> cancelledOrders,
        DateTime? startDate,
        DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? today.AddDays(-30);
        var end = endDate?.Date ?? today;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayEnd = date.AddDays(1).AddTicks(-1);

            var dayAllOrders = allOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= date && vietnamTime <= dayEnd;
            }).ToList();

            var dayCancelledOrders = cancelledOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= date && vietnamTime <= dayEnd;
            }).ToList();

            var cancelledCount = dayCancelledOrders.Count;
            var totalCount = dayAllOrders.Count;
            var rate = totalCount > 0 ? Math.Round((decimal)cancelledCount / totalCount * 100, 2) : 0;

            result.Labels.Add(date.ToString("dd/MM"));
            result.CancelledData.Add(cancelledCount);
            result.CancellationRateData.Add(rate);
        }
    }


    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo tuần
    /// </summary>
    private static void BuildWeeklyTrend(
        CancelledOrdersTrendViewModel result,
        List<Order> allOrders,
        List<Order> cancelledOrders,
        DateTime? startDate,
        DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? today.AddDays(-84); // 12 weeks
        var end = endDate?.Date ?? today;

        // Find the Monday of the start week
        int daysToMonday = ((int)start.DayOfWeek - 1 + 7) % 7;
        var weekStart = start.AddDays(-daysToMonday);

        while (weekStart <= end)
        {
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var weekAllOrders = allOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= weekStart && vietnamTime <= weekEnd;
            }).ToList();

            var weekCancelledOrders = cancelledOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= weekStart && vietnamTime <= weekEnd;
            }).ToList();

            var cancelledCount = weekCancelledOrders.Count;
            var totalCount = weekAllOrders.Count;
            var rate = totalCount > 0 ? Math.Round((decimal)cancelledCount / totalCount * 100, 2) : 0;

            result.Labels.Add($"W{GetWeekOfYear(weekStart)}");
            result.CancelledData.Add(cancelledCount);
            result.CancellationRateData.Add(rate);

            weekStart = weekStart.AddDays(7);
        }
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo tháng
    /// </summary>
    private static void BuildMonthlyTrend(
        CancelledOrdersTrendViewModel result,
        List<Order> allOrders,
        List<Order> cancelledOrders,
        DateTime? startDate,
        DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? new DateTime(today.Year, 1, 1);
        var end = endDate?.Date ?? today;

        var monthStart = new DateTime(start.Year, start.Month, 1);
        var monthEnd = new DateTime(end.Year, end.Month, 1).AddMonths(1).AddTicks(-1);

        while (monthStart <= monthEnd)
        {
            var nextMonth = monthStart.AddMonths(1);

            var monthAllOrders = allOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= monthStart && vietnamTime < nextMonth;
            }).ToList();

            var monthCancelledOrders = cancelledOrders.Where(o =>
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= monthStart && vietnamTime < nextMonth;
            }).ToList();

            var cancelledCount = monthCancelledOrders.Count;
            var totalCount = monthAllOrders.Count;
            var rate = totalCount > 0 ? Math.Round((decimal)cancelledCount / totalCount * 100, 2) : 0;

            result.Labels.Add(monthStart.ToString("MM/yyyy"));
            result.CancelledData.Add(cancelledCount);
            result.CancellationRateData.Add(rate);

            monthStart = nextMonth;
        }
    }

    /// <summary>
    /// Lấy số tuần trong năm
    /// </summary>
    private static int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    #endregion
}
