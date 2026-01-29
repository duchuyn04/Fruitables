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

        // Get all completed orders with items for consistent calculation
        var completedOrdersWithItems = await GetCompletedOrdersWithItemsAsync();
        
        // Get refunded orders with items for consistent calculation using CalculateRevenueFromItems
        var refundedOrdersWithItems = await GetRefundedOrdersWithItemsAsync();

        // Calculate revenues using Order.Total for consistency (Requirements: 1.2)
        var totalRevenue = CalculateRevenueFromOrders(completedOrdersWithItems) 
                         - CalculateRevenueFromOrders(refundedOrdersWithItems);
        
        var todayFiltered = FilterByDateRange(completedOrdersWithItems, todayStart, todayEnd);
        var todayRevenue = CalculateRevenueFromOrders(todayFiltered)
                         - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, todayStart, todayEnd));
        
        var weekFiltered = FilterByDateRange(completedOrdersWithItems, weekStart, weekEnd);
        var weeklyRevenue = CalculateRevenueFromOrders(weekFiltered)
                          - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, weekStart, weekEnd));
        
        var monthFiltered = FilterByDateRange(completedOrdersWithItems, monthStart, monthEnd);
        var monthlyRevenue = CalculateRevenueFromOrders(monthFiltered)
                           - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, monthStart, monthEnd));
        
        var yearFiltered = FilterByDateRange(completedOrdersWithItems, yearStart, yearEnd);
        var yearlyRevenue = CalculateRevenueFromOrders(yearFiltered)
                          - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, yearStart, yearEnd));

        // Calculate previous period revenues for growth comparison
        var lastMonthFiltered = FilterByDateRange(completedOrdersWithItems, lastMonthStart, lastMonthEnd);
        var lastMonthRevenue = CalculateRevenueFromOrders(lastMonthFiltered)
                             - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, lastMonthStart, lastMonthEnd));
        
        var lastWeekFiltered = FilterByDateRange(completedOrdersWithItems, lastWeekStart, lastWeekEnd);
        var lastWeekRevenue = CalculateRevenueFromOrders(lastWeekFiltered)
                            - CalculateRevenueFromOrders(FilterByDateRange(refundedOrdersWithItems, lastWeekStart, lastWeekEnd));

        // Calculate growth percentages
        var monthlyGrowth = CalculateGrowthPercent(monthlyRevenue, lastMonthRevenue);
        var weeklyGrowth = CalculateGrowthPercent(weeklyRevenue, lastWeekRevenue);

        // Calculate order counts
        var totalOrders = completedOrdersWithItems.Count;
        var todayOrders = todayFiltered.Count;

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

    /// <summary>
    /// Tính doanh thu từ Order.Total (bao gồm phí ship, đã trừ discount)
    /// </summary>
    private static decimal CalculateRevenueFromOrders(List<Order> orders)
    {
        return orders.Sum(o => o.Total);
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

        // Get completed and refunded orders with items for consistent calculation (Requirements: 1.1, 1.3)
        var completedOrdersWithItems = await GetCompletedOrdersWithItemsAsync();
        var refundedOrdersWithItems = await GetRefundedOrdersWithItemsAsync();

        // Filter by date range
        var filteredCompleted = FilterByDateRange(completedOrdersWithItems, startDate, endDate);
        var filteredRefunded = FilterByDateRange(refundedOrdersWithItems, startDate, endDate);

        // Calculate revenue using CalculateRevenueFromOrders for consistency with GetRevenueOverviewAsync
        var revenue = CalculateRevenueFromOrders(filteredCompleted) - CalculateRevenueFromOrders(filteredRefunded);
        var orderCount = filteredCompleted.Count;
        var aov = orderCount > 0 ? revenue / orderCount : 0;

        // Return result with all values (0 if no orders - never null)
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
        var completedOrders = await GetCompletedOrdersWithItemsAsync();
        
        if (startDate.HasValue && endDate.HasValue)
        {
            completedOrders = FilterByDateRange(completedOrders, startDate.Value, endDate.Value);
        }

        // Group by category
        var categoryRevenues = completedOrders
            .SelectMany(o => o.Items)
            .Where(i => i.Product?.Category != null)
            .GroupBy(i => new { i.Product!.CategoryId, i.Product.Category!.Name })
            .Select(g => new CategoryRevenueItem
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Revenue = g.Sum(i => i.Total),
                QuantitySold = g.Sum(i => i.Quantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
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
        var completedOrders = await GetCompletedOrdersWithItemsAsync();
        
        if (startDate.HasValue && endDate.HasValue)
        {
            completedOrders = FilterByDateRange(completedOrders, startDate.Value, endDate.Value);
        }

        var productQuery = completedOrders
            .SelectMany(o => o.Items)
            .Where(i => i.Product != null);

        if (categoryId.HasValue)
        {
            productQuery = productQuery.Where(i => i.Product!.CategoryId == categoryId.Value);
        }

        var topProducts = productQuery
            .GroupBy(i => new 
            { 
                i.ProductId, 
                i.ProductName, 
                CategoryName = i.Product?.Category?.Name ?? "Không có danh mục" 
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
        var completedOrders = await GetCompletedOrdersAsync();
        
        if (startDate.HasValue && endDate.HasValue)
        {
            completedOrders = FilterByDateRange(completedOrders, startDate.Value, endDate.Value);
        }

        var result = new RevenueTrendViewModel { Period = period };

        switch (period)
        {
            case TrendPeriod.Daily:
                BuildDailyTrend(result, completedOrders, startDate, endDate);
                break;
            case TrendPeriod.Weekly:
                BuildWeeklyTrend(result, completedOrders, startDate, endDate);
                break;
            case TrendPeriod.Monthly:
                BuildMonthlyTrend(result, completedOrders, startDate, endDate);
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
        var completedOrders = await GetCompletedOrdersAsync();
        var refundedOrders = await GetRefundedOrdersAsync();

        var currentCompleted = FilterByDateRange(completedOrders, currentStart, currentEnd);
        var currentRefunded = FilterByDateRange(refundedOrders, currentStart, currentEnd);
        var currentRevenue = CalculateNetRevenue(currentCompleted, currentRefunded);
        var currentOrders = currentCompleted.Count;

        var previousCompleted = FilterByDateRange(completedOrders, previousStart, previousEnd);
        var previousRefunded = FilterByDateRange(refundedOrders, previousStart, previousEnd);
        var previousRevenue = CalculateNetRevenue(previousCompleted, previousRefunded);
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
    /// Lấy tất cả đơn hàng hoàn thành (PaymentStatus = Paid VÀ OrderStatus = Delivered)
    /// </summary>
    private async Task<List<Order>> GetCompletedOrdersAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tất cả đơn hàng hoàn thành kèm theo items và product info
    /// </summary>
    private async Task<List<Order>> GetCompletedOrdersWithItemsAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Category)
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == OrderStatus.Delivered)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tất cả đơn hàng đã hoàn tiền (PaymentStatus = Refunded)
    /// Chỉ lấy đơn Returned (đã giao rồi trả lại) - không lấy đơn Cancelled
    /// vì đơn Cancelled chưa bao giờ được tính vào doanh thu
    /// </summary>
    private async Task<List<Order>> GetRefundedOrdersAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tất cả đơn hàng đã hoàn tiền kèm theo items (PaymentStatus = Refunded)
    /// Chỉ lấy đơn Returned (đã giao rồi trả lại) - không lấy đơn Cancelled
    /// vì đơn Cancelled chưa bao giờ được tính vào doanh thu
    /// </summary>
    private async Task<List<Order>> GetRefundedOrdersWithItemsAsync()
    {
        return await _unitOfWork.Orders.Query()
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Category)
            .Where(o => o.PaymentStatus == PaymentStatus.Refunded && o.Status == OrderStatus.Returned)
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
            // Convert UTC to Vietnam time for comparison
            var vietnamTime = ConvertToVietnamTime(o.CreatedAt);
            return vietnamTime >= startDate && vietnamTime <= endDate;
        }).ToList();
    }

    /// <summary>
    /// Chuyển đổi DateTime UTC sang giờ Việt Nam (UTC+7)
    /// </summary>
    private static DateTime ConvertToVietnamTime(DateTime utcDateTime)
    {
        // Nếu DateTime đã là Local hoặc Unspecified, giả định là UTC
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
            // Linux/macOS
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, vietnamTimeZone);
        }
    }

    /// <summary>
    /// Tính doanh thu thuần = Tổng (Subtotal - Discount) của đơn hoàn thành - Tổng đơn refunded
    /// </summary>
    private static decimal CalculateNetRevenue(List<Order> completedOrders, List<Order> refundedOrders)
    {
        var completedRevenue = completedOrders.Sum(o => o.Subtotal - o.Discount);
        var refundedAmount = refundedOrders.Sum(o => o.Subtotal - o.Discount);
        return completedRevenue - refundedAmount;
    }

    /// <summary>
    /// Tính doanh thu thuần từ danh sách đơn hàng (không trừ refund)
    /// </summary>
    private static decimal CalculateNetRevenue(List<Order> orders)
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
    private static void BuildDailyTrend(RevenueTrendViewModel result, List<Order> orders, DateTime? startDate, DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? today.AddDays(-30);
        var end = endDate?.Date ?? today;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayEnd = date.AddDays(1).AddTicks(-1);
            // Filter orders by Vietnam time (consistent with FilterByDateRange)
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
    private static void BuildWeeklyTrend(RevenueTrendViewModel result, List<Order> orders, DateTime? startDate, DateTime? endDate)
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
            // Filter orders by Vietnam time (consistent with FilterByDateRange)
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
    private static void BuildMonthlyTrend(RevenueTrendViewModel result, List<Order> orders, DateTime? startDate, DateTime? endDate)
    {
        var today = DateRangePresetExtensions.GetVietnamToday();
        var start = startDate?.Date ?? new DateTime(today.Year, 1, 1);
        var end = endDate?.Date ?? today;

        var monthStart = new DateTime(start.Year, start.Month, 1);
        var monthEnd = new DateTime(end.Year, end.Month, 1).AddMonths(1).AddTicks(-1);

        while (monthStart <= monthEnd)
        {
            var nextMonth = monthStart.AddMonths(1);
            // Filter orders by Vietnam time (consistent with FilterByDateRange)
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
}
