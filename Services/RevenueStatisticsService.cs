using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Services;

/// <summary>
/// Service xử lý thống kê doanh thu chi tiết.
/// Doanh thu thuần (Net Revenue) = Sum(Subtotal - Discount) của các đơn có:
/// - PaymentStatus = Paid VÀ OrderStatus = Delivered
/// - Trừ đi giá trị các đơn đã Refunded
/// </summary>
public class RevenueStatisticsService : IRevenueStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public RevenueStatisticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<RevenueOverviewViewModel> GetRevenueOverviewAsync()
    {
        // Calculate date ranges using presets
        var (todayStart, todayEnd) = DateRangePreset.Today.ToDateRange();
        var (weekStart, weekEnd) = DateRangePreset.Last7Days.ToDateRange();
        var (monthStart, monthEnd) = DateRangePreset.ThisMonth.ToDateRange();
        var (yearStart, yearEnd) = DateRangePreset.ThisYear.ToDateRange();
        var (lastMonthStart, lastMonthEnd) = DateRangePreset.LastMonth.ToDateRange();
        var (lastWeekStart, lastWeekEnd) = DateRangePreset.LastWeek.ToDateRange();

        // Materialize decimal columns once and reuse — SumAsync on decimal fails on SQLite.
        var completedTotals = await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Select(o => new { o.Total, o.CreatedAt })
            .ToListAsync();

        var refundedTotals = await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned)
            .Select(o => new { o.Total, o.CreatedAt })
            .ToListAsync();

        var totalCompletedRevenue = completedTotals.Sum(o => o.Total);
        var totalRefundedRevenue = refundedTotals.Sum(o => o.Total);
        var totalRevenue = totalCompletedRevenue - totalRefundedRevenue;

        // Today
        var (todayStartNorm, todayExclEnd) = NormalizeStoredVietnamRange(todayStart, todayEnd);
        var todayCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= todayStartNorm && o.CreatedAt < todayExclEnd).Sum(o => o.Total);
        var todayRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= todayStartNorm && o.CreatedAt < todayExclEnd).Sum(o => o.Total);
        var todayRevenue = todayCompletedRevenue - todayRefundedRevenue;

        // Last 7 days
        var (weekStartNorm, weekExclEnd) = NormalizeStoredVietnamRange(weekStart, weekEnd);
        var weeklyCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= weekStartNorm && o.CreatedAt < weekExclEnd).Sum(o => o.Total);
        var weeklyRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= weekStartNorm && o.CreatedAt < weekExclEnd).Sum(o => o.Total);
        var weeklyRevenue = weeklyCompletedRevenue - weeklyRefundedRevenue;

        // This Month
        var (monthStartNorm, monthExclEnd) = NormalizeStoredVietnamRange(monthStart, monthEnd);
        var monthlyCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= monthStartNorm && o.CreatedAt < monthExclEnd).Sum(o => o.Total);
        var monthlyRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= monthStartNorm && o.CreatedAt < monthExclEnd).Sum(o => o.Total);
        var monthlyRevenue = monthlyCompletedRevenue - monthlyRefundedRevenue;

        // This Year
        var (yearStartNorm, yearExclEnd) = NormalizeStoredVietnamRange(yearStart, yearEnd);
        var yearlyCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= yearStartNorm && o.CreatedAt < yearExclEnd).Sum(o => o.Total);
        var yearlyRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= yearStartNorm && o.CreatedAt < yearExclEnd).Sum(o => o.Total);
        var yearlyRevenue = yearlyCompletedRevenue - yearlyRefundedRevenue;

        // Last Month
        var (lastMonthStartNorm, lastMonthExclEnd) = NormalizeStoredVietnamRange(lastMonthStart, lastMonthEnd);
        var lastMonthCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= lastMonthStartNorm && o.CreatedAt < lastMonthExclEnd).Sum(o => o.Total);
        var lastMonthRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= lastMonthStartNorm && o.CreatedAt < lastMonthExclEnd).Sum(o => o.Total);
        var lastMonthRevenue = lastMonthCompletedRevenue - lastMonthRefundedRevenue;

        // Last Week
        var (lastWeekStartNorm, lastWeekExclEnd) = NormalizeStoredVietnamRange(lastWeekStart, lastWeekEnd);
        var lastWeekCompletedRevenue = completedTotals.Where(o => o.CreatedAt >= lastWeekStartNorm && o.CreatedAt < lastWeekExclEnd).Sum(o => o.Total);
        var lastWeekRefundedRevenue = refundedTotals.Where(o => o.CreatedAt >= lastWeekStartNorm && o.CreatedAt < lastWeekExclEnd).Sum(o => o.Total);
        var lastWeekRevenue = lastWeekCompletedRevenue - lastWeekRefundedRevenue;

        // Calculate growth percentages
        var monthlyGrowth = CalculateGrowthPercent(monthlyRevenue, lastMonthRevenue);
        var weeklyGrowth = CalculateGrowthPercent(weeklyRevenue, lastWeekRevenue);

        // Order counts
        var totalOrders = completedTotals.Count;
        var todayOrders = completedTotals.Count(o => o.CreatedAt >= todayStartNorm && o.CreatedAt < todayExclEnd);

        var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        return new RevenueOverviewViewModel
        {
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            WeeklyRevenue = weeklyRevenue,
            MonthlyRevenue = monthlyRevenue,
            YearlyRevenue = yearlyRevenue,
            MonthlyGrowthPercent = monthlyGrowth,
            WeeklyGrowthPercent = weeklyGrowth,
            TotalOrders = totalOrders,
            TodayOrders = todayOrders,
            AverageOrderValue = aov
        };
    }

    /// <inheritdoc />
    public async Task<RevenueByDateRangeResult> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        // Validate date range: StartDate must be <= EndDate
        if (startDate > endDate)
        {
            return RevenueByDateRangeResult.ValidationError(
                $"Ngày bắt đầu ({startDate:dd/MM/yyyy}) không được lớn hơn ngày kết thúc ({endDate:dd/MM/yyyy}).");
        }

        var (normStart, exclEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var completedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < exclEnd);

        var refundedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned)
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < exclEnd);

        var completedTotals = await completedQuery.Select(o => o.Total).ToListAsync();
        var refundedTotals = await refundedQuery.Select(o => o.Total).ToListAsync();
        var revenue = completedTotals.Sum() - refundedTotals.Sum();
        var orderCount = completedTotals.Count;
        var aov = orderCount > 0 ? revenue / orderCount : 0;

        return RevenueByDateRangeResult.Success(new RevenueOverviewViewModel
        {
            TotalRevenue = revenue,
            TodayRevenue = 0,
            WeeklyRevenue = 0,
            MonthlyRevenue = 0,
            YearlyRevenue = 0,
            MonthlyGrowthPercent = 0,
            WeeklyGrowthPercent = 0,
            TotalOrders = orderCount,
            TodayOrders = 0,
            AverageOrderValue = aov
        });
    }

    /// <inheritdoc />
    public async Task<RevenueByCategoryViewModel> GetRevenueByCategoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var (normStart, exclEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < exclEnd);

        var itemRows = await query
            .SelectMany(o => o.Items)
            .Where(i => i.Product != null && i.Product.Category != null)
            .Select(i => new
            {
                CategoryId = i.Product!.CategoryId,
                CategoryName = i.Product.Category!.Name,
                i.OrderId,
                i.Quantity,
                i.Total
            })
            .ToListAsync();

        var categoryRevenues = itemRows
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new CategoryRevenueItem
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName ?? string.Empty,
                Revenue = g.Sum(x => x.Total),
                QuantitySold = g.Sum(x => x.Quantity),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count()
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        var totalRevenue = categoryRevenues.Sum(c => c.Revenue);

        // Calculate percentages
        foreach (var category in categoryRevenues)
        {
            category.Percentage = totalRevenue > 0 
                ? Math.Round((category.Revenue / totalRevenue) * 100, 2) 
                : 0;
        }

        return new RevenueByCategoryViewModel
        {
            Categories = categoryRevenues,
            TotalRevenue = totalRevenue,
            Filter = new RevenueFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            }
        };
    }

    /// <inheritdoc />
    public async Task<TopProductsViewModel> GetTopProductsAsync(
        int topCount = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? categoryId = null)
    {
        var (normStart, exclEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < exclEnd);

        var itemQuery = query.SelectMany(o => o.Items)
            .Where(i => i.Product != null);

        if (categoryId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.Product!.CategoryId == categoryId.Value);
        }

        var itemRows = await itemQuery
            .Select(i => new
            {
                i.ProductId,
                i.ProductName,
                CategoryName = i.Product!.Category.Name ?? "Không có danh mục",
                i.Quantity,
                i.Total
            })
            .ToListAsync();

        var topProducts = itemRows
            .GroupBy(i => new { i.ProductId, i.ProductName, i.CategoryName })
            .Select(g => new TopProductItem
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                Revenue = g.Sum(i => i.Total),
                QuantitySold = g.Sum(i => i.Quantity),
                AveragePrice = g.Sum(i => i.Quantity) > 0
                    ? g.Sum(i => i.Total) / g.Sum(i => i.Quantity)
                    : 0
            })
            .OrderByDescending(p => p.Revenue)
            .Take(topCount)
            .ToList();

        return new TopProductsViewModel
        {
            Products = topProducts,
            TopCount = topCount,
            Filter = new RevenueFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                CategoryId = categoryId
            }
        };
    }

    /// <inheritdoc />
    public async Task<RevenueTrendViewModel> GetRevenueTrendAsync(
        TrendPeriod period,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var (normStart, exclEnd) = NormalizeStoredVietnamRange(startDate, endDate);

        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Where(o => o.CreatedAt >= normStart && o.CreatedAt < exclEnd);

        var orders = await query
            .Select(o => new OrderTrendDto
            {
                CreatedAt = o.CreatedAt,
                Subtotal = o.Subtotal,
                Discount = o.Discount
            })
            .ToListAsync();

        var result = new RevenueTrendViewModel { Period = period };

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

        return result;
    }

    /// <inheritdoc />
    public async Task<PeriodComparisonViewModel> ComparePeriodsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        DateTime previousStart,
        DateTime previousEnd)
    {
        var (_, currentExclEnd) = NormalizeStoredVietnamRange(currentStart, currentEnd);
        var (_, previousExclEnd) = NormalizeStoredVietnamRange(previousStart, previousEnd);

        var completedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        var refundedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned);

        // Fetch current period
        var currentCompleted = await completedQuery
            .Where(o => o.CreatedAt >= currentStart && o.CreatedAt < currentExclEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var currentRefunded = await refundedQuery
            .Where(o => o.CreatedAt >= currentStart && o.CreatedAt < currentExclEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var currentRevenue = currentCompleted.Sum(o => o.Subtotal - o.Discount) 
                             - currentRefunded.Sum(o => o.Subtotal - o.Discount);
        var currentOrders = currentCompleted.Count;

        // Fetch previous period
        var previousCompleted = await completedQuery
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousExclEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var previousRefunded = await refundedQuery
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < previousExclEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var previousRevenue = previousCompleted.Sum(o => o.Subtotal - o.Discount) 
                              - previousRefunded.Sum(o => o.Subtotal - o.Discount);
        var previousOrders = previousCompleted.Count;

        var growthPercent = CalculateGrowthPercent(currentRevenue, previousRevenue);
        var growthAmount = currentRevenue - previousRevenue;

        return new PeriodComparisonViewModel
        {
            CurrentPeriodRevenue = currentRevenue,
            PreviousPeriodRevenue = previousRevenue,
            GrowthPercent = growthPercent,
            GrowthAmount = growthAmount,
            CurrentPeriodOrders = currentOrders,
            PreviousPeriodOrders = previousOrders,
            CurrentPeriodLabel = $"{currentStart:dd/MM/yyyy} - {currentEnd:dd/MM/yyyy}",
            PreviousPeriodLabel = $"{previousStart:dd/MM/yyyy} - {previousEnd:dd/MM/yyyy}"
        };
    }

    #region Helper Methods

    /// <summary>
    /// Lấy ngày đơn hàng đầu tiên trong hệ thống (dùng cho AllTime preset)
    /// </summary>
    public async Task<DateTime?> GetFirstOrderDateAsync()
    {
        var firstOrder = await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .OrderBy(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        return firstOrder?.CreatedAt;
    }

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
                ? end.Date.AddDays(1)
                : end.AddTicks(1);
        }

        return (start, exclusiveEnd);
    }

    private static decimal CalculateNetRevenue(List<OrderTrendDto> orders)
    {
        return orders.Sum(o => o.Subtotal - o.Discount);
    }

    /// <summary>
    /// Tính phần trăm tăng trưởng
    /// </summary>
    private static decimal CalculateGrowthPercent(decimal currentRevenue, decimal previousRevenue)
    {
        if (previousRevenue == 0)
        {
            return currentRevenue > 0 ? 100 : 0;
        }
        return Math.Round(((currentRevenue - previousRevenue) / previousRevenue) * 100, 2);
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo ngày
    /// </summary>
    private static void BuildDailyTrend(RevenueTrendViewModel result, List<OrderTrendDto> orders, DateTime? startDate, DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? today.AddDays(-30);
        var end = endDate?.Date ?? today;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayEnd = date.AddDays(1).AddTicks(-1);
            var dayOrders = orders.Where(o => o.CreatedAt >= date && o.CreatedAt <= dayEnd).ToList();

            result.Labels.Add(date.ToString("dd/MM"));
            result.RevenueData.Add(CalculateNetRevenue(dayOrders));
            result.OrdersData.Add(dayOrders.Count);
        }
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo tuần
    /// </summary>
    private static void BuildWeeklyTrend(RevenueTrendViewModel result, List<OrderTrendDto> orders, DateTime? startDate, DateTime? endDate)
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
            var weekOrders = orders.Where(o => o.CreatedAt >= weekStart && o.CreatedAt <= weekEnd).ToList();

            result.Labels.Add($"W{GetWeekOfYear(weekStart)}");
            result.RevenueData.Add(CalculateNetRevenue(weekOrders));
            result.OrdersData.Add(weekOrders.Count);

            weekStart = weekStart.AddDays(7);
        }
    }

    /// <summary>
    /// Xây dựng dữ liệu xu hướng theo tháng
    /// </summary>
    private static void BuildMonthlyTrend(RevenueTrendViewModel result, List<OrderTrendDto> orders, DateTime? startDate, DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? new DateTime(today.Year, 1, 1);
        var end = endDate?.Date ?? today;

        var monthStart = new DateTime(start.Year, start.Month, 1);
        var monthEnd = new DateTime(end.Year, end.Month, 1).AddMonths(1).AddTicks(-1);

        while (monthStart <= monthEnd)
        {
            var nextMonth = monthStart.AddMonths(1);
            var monthOrders = orders.Where(o => o.CreatedAt >= monthStart && o.CreatedAt < nextMonth).ToList();

            result.Labels.Add(monthStart.ToString("MM/yyyy"));
            result.RevenueData.Add(CalculateNetRevenue(monthOrders));
            result.OrdersData.Add(monthOrders.Count);

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
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
    }
}
