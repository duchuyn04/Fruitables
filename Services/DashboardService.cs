using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(
        ChartPeriod chartPeriod = ChartPeriod.Last7Days, 
        int lowStockThreshold = 10)
    {
        var revenue = await GetRevenueStatisticsAsync();
        var orders = await GetOrderStatisticsAsync();
        var inventory = await GetInventoryStatisticsAsync(lowStockThreshold);
        var growthChart = await GetGrowthChartDataAsync(chartPeriod);
        var recentOrders = await GetRecentOrdersAsync(5);

        return new DashboardViewModel
        {
            Revenue = revenue,
            Orders = orders,
            Inventory = inventory,
            GrowthChart = growthChart,
            RecentOrders = recentOrders
        };
    }

    public async Task<List<RecentOrderItem>> GetRecentOrdersAsync(int count = 5)
    {
        var orders = await _unitOfWork.Orders.Query()
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync();

        return orders.Select(o => new RecentOrderItem
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.User?.Name ?? "Khách vãng lai",
            Total = o.Total,
            Status = GetStatusDisplayName(o.Status),
            StatusBadgeClass = GetStatusBadgeClass(o.Status),
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    private static string GetStatusDisplayName(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xử lý",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Shipped => "Đang giao",
        OrderStatus.Delivered => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static string GetStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-secondary",
        OrderStatus.Processing => "bg-info",
        OrderStatus.Shipped => "bg-warning",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public async Task<RevenueStatistics> GetRevenueStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);

        var paidOrders = await _unitOfWork.Orders.Query()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .ToListAsync();

        var totalRevenue = paidOrders.Sum(o => o.Total);

        var todayRevenue = paidOrders
            .Where(o => o.CreatedAt.Date == today)
            .Sum(o => o.Total);

        var monthlyRevenue = paidOrders
            .Where(o => o.CreatedAt >= firstDayOfMonth)
            .Sum(o => o.Total);

        var lastMonthRevenue = paidOrders
            .Where(o => o.CreatedAt >= firstDayOfLastMonth && o.CreatedAt < firstDayOfMonth)
            .Sum(o => o.Total);

        decimal growthPercent = 0;
        if (lastMonthRevenue > 0)
        {
            growthPercent = ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
        }
        else if (monthlyRevenue > 0)
        {
            growthPercent = 100; // Từ 0 lên có doanh thu = 100% growth
        }

        return new RevenueStatistics
        {
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            MonthlyRevenue = monthlyRevenue,
            MonthlyGrowthPercent = Math.Round(growthPercent, 2)
        };
    }

    public async Task<OrderStatistics> GetOrderStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var orders = await _unitOfWork.Orders.Query().ToListAsync();

        return new OrderStatistics
        {
            TotalOrders = orders.Count,
            TodayOrders = orders.Count(o => o.CreatedAt >= today && o.CreatedAt < tomorrow),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
            DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled)
        };
    }

    public async Task<InventoryStatistics> GetInventoryStatisticsAsync(int lowStockThreshold = 10)
    {
        // Xử lý threshold âm
        var threshold = Math.Max(0, lowStockThreshold);

        var products = await _unitOfWork.Products.Query().ToListAsync();
        var activeProducts = products.Where(p => p.IsActive).ToList();

        return new InventoryStatistics
        {
            TotalProducts = products.Count,
            ActiveProducts = activeProducts.Count,
            OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),
            LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= threshold),
            TotalStockQuantity = activeProducts.Sum(p => p.StockQuantity)
        };
    }

    public async Task<GrowthChartData> GetGrowthChartDataAsync(ChartPeriod period)
    {
        var result = new GrowthChartData();
        var today = DateTime.UtcNow.Date;

        var paidOrders = await _unitOfWork.Orders.Query()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid)
            .ToListAsync();

        switch (period)
        {
            case ChartPeriod.Last7Days:
                result = BuildDailyChartData(paidOrders, today, 7);
                break;
            case ChartPeriod.Last30Days:
                result = BuildDailyChartData(paidOrders, today, 30);
                break;
            case ChartPeriod.Last12Months:
                result = BuildMonthlyChartData(paidOrders, today, 12);
                break;
        }

        return result;
    }

    private static GrowthChartData BuildDailyChartData(List<Order> orders, DateTime today, int days)
    {
        var result = new GrowthChartData();

        for (int i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayOrders = orders.Where(o => o.CreatedAt.Date == date).ToList();

            result.Labels.Add(date.ToString("dd/MM"));
            result.RevenueData.Add(dayOrders.Sum(o => o.Total));
            result.OrdersData.Add(dayOrders.Count);
        }

        return result;
    }

    private static GrowthChartData BuildMonthlyChartData(List<Order> orders, DateTime today, int months)
    {
        var result = new GrowthChartData();

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthOrders = orders.Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd).ToList();

            result.Labels.Add(monthStart.ToString("MM/yyyy"));
            result.RevenueData.Add(monthOrders.Sum(o => o.Total));
            result.OrdersData.Add(monthOrders.Count);
        }

        return result;
    }
}
