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

        var (normStart, normEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var allQuery = _unitOfWork.Orders.Query().AsNoTracking()
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < normEnd);
        var cancelledQuery = _unitOfWork.Orders.Query().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled && o.CreatedAt >= normStart && o.CreatedAt < normEnd);

        var totalOrders = await allQuery.CountAsync();
        var totalCancelled = await cancelledQuery.CountAsync();

        // SQLite does not support Sum on decimal; materialize and sum in memory.
        var totalValues = await cancelledQuery.Select(o => o.Total).ToListAsync();
        var totalCancelledValue = totalValues.Sum();

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

        var (normStart, normEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var query = _unitOfWork.Orders.Query().AsNoTracking()
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < normEnd);

        var orders = await query
            .Select(o => new OrderTrendDto
            {
                CreatedAt = o.CreatedAt,
                Status = o.Status
            })
            .ToListAsync();

        var result = new CancelledOrdersTrendViewModel { Period = period };

        switch (period)
        {
            case TrendPeriod.Daily:
                BuildDailyTrend(result, orders, startDate, endDate);
                break;
            case TrendPeriod.Weekly:
                BuildWeeklyTrend(result, orders, startDate, endDate);
                break;
            case TrendPeriod.Monthly:
                BuildMonthlyTrend(result, orders, startDate, endDate);
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

        var (normStart, normEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled && o.CreatedAt >= normStart && o.CreatedAt < normEnd);

        var totalCancelled = await query.CountAsync();

        // Materialize just the reasons (small set vs full order) and normalize/group in memory.
        // EF Core's GroupBy + string.IsNullOrWhiteSpace translation is provider-dependent, so
        // we keep the aggregation deterministic across SQL Server, SQLite, and InMemory.
        var reasonRows = await query
            .Select(o => o.CancelReason)
            .ToListAsync();

        var reasonGroups = reasonRows
            .GroupBy(reason => string.IsNullOrWhiteSpace(reason) ? "Không có lý do" : reason)
            .Select(g => new
            {
                Reason = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        var reasonItems = reasonGroups.Select(r => new CancelReasonItem
        {
            Reason = r.Reason,
            Count = r.Count,
            Percentage = totalCancelled > 0
                ? Math.Round((decimal)r.Count / totalCancelled * 100, 2)
                : 0
        }).ToList();

        return CancelledOrdersResult<CancelReasonStatisticsViewModel>.Success(new CancelReasonStatisticsViewModel
        {
            Reasons = reasonItems,
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

        var cancelledQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled);

        var (_, currentExclEnd) = NormalizeStoredVietnamRange(currentStart, currentEnd);
        var (_, previousExclEnd) = NormalizeStoredVietnamRange(previousStart, previousEnd);

        var currentCount = await cancelledQuery
            .Where(o => o.CreatedAt >= currentStart && o.CreatedAt < currentExclEnd)
            .CountAsync();

        var previousCount = await cancelledQuery
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousExclEnd)
            .CountAsync();

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
    /// Chuẩn hóa range nửa-mở theo stored Vietnam time: start giữ nguyên, end date-only → AddDays(1),
    /// end có time → +1 tick. Kết quả trả về (start, exclusiveEnd) cho WHERE CreatedAt >= start AND CreatedAt &lt; exclusiveEnd.
    /// Order.CreatedAt được stored Vietnam time, không convert UTC lần nữa.
    /// </summary>
    private static (DateTime start, DateTime exclusiveEnd) NormalizeStoredVietnamRange(
        DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.MinValue;
        DateTime exclusiveEnd;

        if (!endDate.HasValue)
        {
            exclusiveEnd = DateTime.MaxValue;
        }
        else
        {
            var end = endDate.Value;
            exclusiveEnd = end.TimeOfDay == TimeSpan.Zero
                ? end.Date.AddDays(1)   // date-only: 00:00 → next day 00:00
                : end.AddTicks(1);      // has time: advance 1 tick for half-open
        }

        return (start, exclusiveEnd);
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo ngày
    /// </summary>
    private static void BuildDailyTrend(
        CancelledOrdersTrendViewModel result,
        List<OrderTrendDto> orders,
        DateTime? startDate,
        DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? today.AddDays(-30);
        var end = endDate?.Date ?? today;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayEnd = date.AddDays(1).AddTicks(-1);

            var dayOrders = orders.Where(o =>
            {
                return o.CreatedAt >= date && o.CreatedAt <= dayEnd;
            }).ToList();

            var cancelledCount = dayOrders.Count(o => o.Status == OrderStatus.Cancelled);
            var totalCount = dayOrders.Count;
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
        List<OrderTrendDto> orders,
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

            var weekOrders = orders.Where(o =>
            {
                return o.CreatedAt >= weekStart && o.CreatedAt <= weekEnd;
            }).ToList();

            var cancelledCount = weekOrders.Count(o => o.Status == OrderStatus.Cancelled);
            var totalCount = weekOrders.Count;
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
        List<OrderTrendDto> orders,
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

            var monthOrders = orders.Where(o =>
            {
                return o.CreatedAt >= monthStart && o.CreatedAt < nextMonth;
            }).ToList();

            var cancelledCount = monthOrders.Count(o => o.Status == OrderStatus.Cancelled);
            var totalCount = monthOrders.Count;
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

    private class OrderTrendDto
    {
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
    }
}
