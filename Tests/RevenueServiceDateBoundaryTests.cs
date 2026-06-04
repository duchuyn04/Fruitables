using Xunit;
using Microsoft.EntityFrameworkCore;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Repositories;
using Fruitables.Services;
using Fruitables.ViewModels;

namespace Fruitables.Tests;

// Guardrail: RevenueStatisticsService dùng ConvertToUtcForQuery trên Order.CreatedAt đã stored
// Vietnam time → shift range sai. Tất cả test này phải FAIL trên implementation hiện tại.
public class RevenueServiceDateBoundaryTests
{
    [Fact]
    public async Task GetRevenueByDateRangeAsync_DateOnlyEndDate_IncludesEveningVietnamOrder()
    {
        // endDate = 2026-06-03 (date-only). ConvertToUtcForQuery(endDate) = 2026-06-03 16:59:59,
        // exclude order 20:00 Vietnam time.
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Orders.AddRange(
            new Order
            {
                Id = 1, OrderNumber = "ORD-1",
                CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 100, Discount = 0, Total = 100
            },
            new Order
            {
                Id = 2, OrderNumber = "ORD-2",
                CreatedAt = new DateTime(2026, 6, 3, 2, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 50, Discount = 0, Total = 50
            },
            new Order
            {
                Id = 3, OrderNumber = "ORD-3",
                CreatedAt = new DateTime(2026, 6, 4, 1, 0, 0),
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                Subtotal = 30, Discount = 0, Total = 30
            }
        );
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var start = new DateTime(2026, 6, 3);
        var end = new DateTime(2026, 6, 3); // date-only
        var result = await service.GetRevenueByDateRangeAsync(start, end);

        Assert.True(result.IsValid);
        Assert.Equal(150, result.Data.TotalRevenue);  // 100 + 50
        Assert.Equal(2, result.Data.TotalOrders);
    }

    [Fact]
    public async Task GetRevenueByCategoryAsync_EveningOrderIncludedInEndDate()
    {
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Categories.Add(new Category { Id = 1, Name = "Fruits", Slug = "fruits" });
        context.Products.Add(new Product
        {
            Id = 1, CategoryId = 1, Name = "Apple", Slug = "apple",
            Price = 10, StockQuantity = 100, MinOrderQuantity = 1, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        context.Orders.Add(new Order
        {
            Id = 1, OrderNumber = "ORD-1",
            CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0),
            Status = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Paid,
            Subtotal = 100, Discount = 0, Total = 100
        });
        context.OrderItems.Add(new OrderItem
        {
            Id = 1, OrderId = 1, ProductId = 1,
            ProductName = "Apple", Price = 10, Quantity = 10, Total = 100
        });
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var result = await service.GetRevenueByCategoryAsync(
            new DateTime(2026, 6, 3), new DateTime(2026, 6, 3));

        Assert.Single(result.Categories);
        Assert.Equal(100, result.Categories[0].Revenue);
    }

    [Fact]
    public async Task GetTopProductsAsync_EveningOrderIncluded()
    {
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Categories.Add(new Category { Id = 1, Name = "Fruits", Slug = "fruits" });
        context.Products.Add(new Product
        {
            Id = 1, CategoryId = 1, Name = "Apple", Slug = "apple",
            Price = 10, StockQuantity = 100, MinOrderQuantity = 1, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        context.Orders.Add(new Order
        {
            Id = 1, OrderNumber = "ORD-1",
            CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0),
            Status = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Paid,
            Subtotal = 200, Discount = 0, Total = 200
        });
        context.OrderItems.Add(new OrderItem
        {
            Id = 1, OrderId = 1, ProductId = 1,
            ProductName = "Apple", Price = 10, Quantity = 20, Total = 200
        });
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var result = await service.GetTopProductsAsync(
            10, new DateTime(2026, 6, 3), new DateTime(2026, 6, 3));

        Assert.Single(result.Products);
        Assert.Equal(200, result.Products[0].Revenue);
    }

    [Fact]
    public async Task GetRevenueTrendAsync_Daily_EveningOrderInCorrectDayBucket()
    {
        // Order 20:00 Vietnam time → phải ở bucket 03/06, không tràn sang 04/06
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Orders.AddRange(
            new Order
            {
                Id = 1, OrderNumber = "ORD-1",
                CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 100, Discount = 0, Total = 100
            },
            new Order
            {
                Id = 2, OrderNumber = "ORD-2",
                CreatedAt = new DateTime(2026, 6, 4, 2, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 50, Discount = 0, Total = 50
            }
        );
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var result = await service.GetRevenueTrendAsync(
            TrendPeriod.Daily,
            new DateTime(2026, 6, 3), new DateTime(2026, 6, 4, 23, 59, 59));

        Assert.Equal(new[] { "03/06", "04/06" }, result.Labels);
        Assert.Equal(2, result.OrdersData.Count);
        Assert.Equal(100, result.RevenueData[0]); // 20:00 order in 03/06
        Assert.Equal(50, result.RevenueData[1]);  // 04/06 order
    }

    [Fact]
    public async Task GetRevenueOverviewAsync_TodayPreset_IncludesEveningOrders()
    {
        // ToDateRange cho Today trả về (today, today.AddDays(1).AddTicks(-1)).
        // ConvertToUtcForQuery shift end date 7h trước, mất order tối.
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Orders.AddRange(
            new Order
            {
                Id = 1, OrderNumber = "ORD-1",
                CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 100, Discount = 0, Total = 100
            },
            new Order
            {
                Id = 2, OrderNumber = "ORD-2",
                CreatedAt = new DateTime(2026, 6, 3, 2, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 50, Discount = 0, Total = 50
            }
        );
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var result = await service.GetRevenueByDateRangeAsync(
            new DateTime(2026, 6, 3), new DateTime(2026, 6, 3, 23, 59, 59));

        Assert.True(result.IsValid);
        Assert.Equal(150, result.Data.TotalRevenue);
        Assert.Equal(2, result.Data.TotalOrders);
    }

    [Fact]
    public async Task GetRevenueByDateRangeAsync_OrdersOutsideRange_NotIncluded()
    {
        var options = TestDbContextFactory.CreateSqliteOptions();
        using var context = new ApplicationDbContext(options);

        context.Orders.AddRange(
            new Order
            {
                Id = 1, OrderNumber = "ORD-IN",
                CreatedAt = new DateTime(2026, 6, 3, 12, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 100, Discount = 0, Total = 100
            },
            new Order
            {
                Id = 2, OrderNumber = "ORD-BEFORE",
                CreatedAt = new DateTime(2026, 5, 31, 23, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 200, Discount = 0, Total = 200
            },
            new Order
            {
                Id = 3, OrderNumber = "ORD-AFTER",
                CreatedAt = new DateTime(2026, 6, 5, 1, 0, 0),
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                Subtotal = 300, Discount = 0, Total = 300
            }
        );
        await context.SaveChangesAsync();

        var service = new RevenueStatisticsService(new UnitOfWork(context));
        var result = await service.GetRevenueByDateRangeAsync(
            new DateTime(2026, 6, 2), new DateTime(2026, 6, 4, 23, 59, 59));

        Assert.True(result.IsValid);
        Assert.Equal(100, result.Data.TotalRevenue);
        Assert.Equal(1, result.Data.TotalOrders);
    }
}
