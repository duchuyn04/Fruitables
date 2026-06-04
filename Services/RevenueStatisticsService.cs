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

        var completedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        var refundedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned);

        // All time
        var totalCompletedRevenue = await completedQuery.SumAsync(o => (decimal?)o.Total) ?? 0;
        var totalRefundedRevenue = await refundedQuery.SumAsync(o => (decimal?)o.Total) ?? 0;
        var totalRevenue = totalCompletedRevenue - totalRefundedRevenue;

        // Today
        var todayUtcStart = ConvertToUtcForQuery(todayStart);
        var todayUtcEnd = ConvertToUtcForQuery(todayEnd);
        var todayCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= todayUtcStart && o.CreatedAt <= todayUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var todayRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= todayUtcStart && o.CreatedAt <= todayUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var todayRevenue = todayCompletedRevenue - todayRefundedRevenue;

        // Last 7 days
        var weekUtcStart = ConvertToUtcForQuery(weekStart);
        var weekUtcEnd = ConvertToUtcForQuery(weekEnd);
        var weeklyCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= weekUtcStart && o.CreatedAt <= weekUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var weeklyRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= weekUtcStart && o.CreatedAt <= weekUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var weeklyRevenue = weeklyCompletedRevenue - weeklyRefundedRevenue;

        // This Month
        var monthUtcStart = ConvertToUtcForQuery(monthStart);
        var monthUtcEnd = ConvertToUtcForQuery(monthEnd);
        var monthlyCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= monthUtcStart && o.CreatedAt <= monthUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var monthlyRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= monthUtcStart && o.CreatedAt <= monthUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var monthlyRevenue = monthlyCompletedRevenue - monthlyRefundedRevenue;

        // This Year
        var yearUtcStart = ConvertToUtcForQuery(yearStart);
        var yearUtcEnd = ConvertToUtcForQuery(yearEnd);
        var yearlyCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= yearUtcStart && o.CreatedAt <= yearUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var yearlyRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= yearUtcStart && o.CreatedAt <= yearUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var yearlyRevenue = yearlyCompletedRevenue - yearlyRefundedRevenue;

        // Last Month
        var lastMonthUtcStart = ConvertToUtcForQuery(lastMonthStart);
        var lastMonthUtcEnd = ConvertToUtcForQuery(lastMonthEnd);
        var lastMonthCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= lastMonthUtcStart && o.CreatedAt <= lastMonthUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var lastMonthRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= lastMonthUtcStart && o.CreatedAt <= lastMonthUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var lastMonthRevenue = lastMonthCompletedRevenue - lastMonthRefundedRevenue;

        // Last Week
        var lastWeekUtcStart = ConvertToUtcForQuery(lastWeekStart);
        var lastWeekUtcEnd = ConvertToUtcForQuery(lastWeekEnd);
        var lastWeekCompletedRevenue = await completedQuery.Where(o => o.CreatedAt >= lastWeekUtcStart && o.CreatedAt <= lastWeekUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var lastWeekRefundedRevenue = await refundedQuery.Where(o => o.CreatedAt >= lastWeekUtcStart && o.CreatedAt <= lastWeekUtcEnd).SumAsync(o => (decimal?)o.Total) ?? 0;
        var lastWeekRevenue = lastWeekCompletedRevenue - lastWeekRefundedRevenue;

        // Calculate growth percentages
        var monthlyGrowth = CalculateGrowthPercent(monthlyRevenue, lastMonthRevenue);
        var weeklyGrowth = CalculateGrowthPercent(weeklyRevenue, lastWeekRevenue);

        // Calculate order counts
        var totalOrders = await completedQuery.CountAsync();
        var todayOrders = await completedQuery.Where(o => o.CreatedAt >= todayUtcStart && o.CreatedAt <= todayUtcEnd).CountAsync();

        // Calculate AOV
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

        var utcStart = ConvertToUtcForQuery(startDate);
        var utcEnd = ConvertToUtcForQuery(endDate);

        var completedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .Where(o => o.CreatedAt >= utcStart && o.CreatedAt <= utcEnd);

        var refundedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned)
            .Where(o => o.CreatedAt >= utcStart && o.CreatedAt <= utcEnd);

        var revenue = (await completedQuery.SumAsync(o => (decimal?)o.Total) ?? 0)
                      - (await refundedQuery.SumAsync(o => (decimal?)o.Total) ?? 0);
        var orderCount = await completedQuery.CountAsync();
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
        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= ConvertToUtcForQuery(startDate.Value));
        }
        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= ConvertToUtcForQuery(endDate.Value));
        }

        var categoryRevenues = await query
            .SelectMany(o => o.Items)
            .Where(i => i.Product != null && i.Product.Category != null)
            .GroupBy(i => new { i.Product!.CategoryId, CategoryName = i.Product.Category!.Name })
            .Select(g => new CategoryRevenueItem
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                Revenue = g.Sum(i => i.Total),
                QuantitySold = g.Sum(i => i.Quantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(c => c.Revenue)
            .ToListAsync();

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
        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= ConvertToUtcForQuery(startDate.Value));
        }
        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= ConvertToUtcForQuery(endDate.Value));
        }

        var itemQuery = query.SelectMany(o => o.Items)
            .Where(i => i.Product != null);

        if (categoryId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.Product!.CategoryId == categoryId.Value);
        }

        var topProducts = await itemQuery
            .GroupBy(i => new 
            { 
                i.ProductId, 
                i.ProductName, 
                CategoryName = i.Product!.Category.Name ?? "Không có danh mục" 
            })
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
            .ToListAsync();

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
        var query = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= ConvertToUtcForQuery(startDate.Value));
        }
        if (endDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= ConvertToUtcForQuery(endDate.Value));
        }

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
        var currentUtcStart = ConvertToUtcForQuery(currentStart);
        var currentUtcEnd = ConvertToUtcForQuery(currentEnd);
        var previousUtcStart = ConvertToUtcForQuery(previousStart);
        var previousUtcEnd = ConvertToUtcForQuery(previousEnd);

        var completedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered);

        var refundedQuery = _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned);

        // Fetch current period
        var currentCompleted = await completedQuery
            .Where(o => o.CreatedAt >= currentUtcStart && o.CreatedAt <= currentUtcEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var currentRefunded = await refundedQuery
            .Where(o => o.CreatedAt >= currentUtcStart && o.CreatedAt <= currentUtcEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var currentRevenue = currentCompleted.Sum(o => o.Subtotal - o.Discount) 
                             - currentRefunded.Sum(o => o.Subtotal - o.Discount);
        var currentOrders = currentCompleted.Count;

        // Fetch previous period
        var previousCompleted = await completedQuery
            .Where(o => o.CreatedAt >= previousUtcStart && o.CreatedAt <= previousUtcEnd)
            .Select(o => new { o.Subtotal, o.Discount })
            .ToListAsync();

        var previousRefunded = await refundedQuery
            .Where(o => o.CreatedAt >= previousUtcStart && o.CreatedAt <= previousUtcEnd)
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

    private static DateTime ConvertToUtcForQuery(DateTime vietnamDateTime)
    {
        // Since Vietnam is UTC+7, subtract 7 hours
        return DateTime.SpecifyKind(vietnamDateTime.AddHours(-7), DateTimeKind.Utc);
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
            var dayOrders = orders.Where(o => 
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= date && vietnamTime <= dayEnd;
            }).ToList();

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
            var weekOrders = orders.Where(o => 
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= weekStart && vietnamTime <= weekEnd;
            }).ToList();

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
            var monthOrders = orders.Where(o => 
            {
                var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
                return vietnamTime >= monthStart && vietnamTime < nextMonth;
            }).ToList();

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
