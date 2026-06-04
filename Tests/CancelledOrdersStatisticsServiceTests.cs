using Xunit;
using Microsoft.EntityFrameworkCore;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Repositories;
using Fruitables.Services;
using Fruitables.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fruitables.Tests
{
    public class CancelledOrdersStatisticsServiceTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetReasonStatisticsAsync_GroupsNullAndEmptyReasonsAs_KhongCoLyDo()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);

            // Seed cancelled orders with different CancelReasons
            var orders = new List<Order>
            {
                new Order { Id = 1, OrderNumber = "ORD-1", Status = OrderStatus.Cancelled, CancelReason = null, CreatedAt = DateTime.UtcNow },
                new Order { Id = 2, OrderNumber = "ORD-2", Status = OrderStatus.Cancelled, CancelReason = "", CreatedAt = DateTime.UtcNow },
                new Order { Id = 3, OrderNumber = "ORD-3", Status = OrderStatus.Cancelled, CancelReason = "   ", CreatedAt = DateTime.UtcNow },
                new Order { Id = 4, OrderNumber = "ORD-4", Status = OrderStatus.Cancelled, CancelReason = "Khách hàng đổi ý", CreatedAt = DateTime.UtcNow },
                new Order { Id = 5, OrderNumber = "ORD-5", Status = OrderStatus.Cancelled, CancelReason = "Khách hàng đổi ý", CreatedAt = DateTime.UtcNow },
                new Order { Id = 6, OrderNumber = "ORD-6", Status = OrderStatus.Cancelled, CancelReason = "Sai thông tin giao hàng", CreatedAt = DateTime.UtcNow },
                new Order { Id = 7, OrderNumber = "ORD-7", Status = OrderStatus.Pending, CancelReason = null, CreatedAt = DateTime.UtcNow } // Not cancelled, should be ignored
            };
            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(context);
            var service = new CancelledOrdersStatisticsService(unitOfWork);

            // Act
            var result = await service.GetReasonStatisticsAsync(null, null);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(6, result.Data.TotalCancelledOrders); // Only cancelled orders

            var reasons = result.Data.Reasons;
            Assert.Equal(3, reasons.Count); // "Khách hàng đổi ý" (2), "Không có lý do" (3 — null + empty + whitespace), "Sai thông tin giao hàng" (1)

            var khongCoLyDo = reasons.FirstOrDefault(r => r.Reason == "Không có lý do");
            var doiY = reasons.FirstOrDefault(r => r.Reason == "Khách hàng đổi ý");
            var saiThongTin = reasons.FirstOrDefault(r => r.Reason == "Sai thông tin giao hàng");

            Assert.NotNull(khongCoLyDo);
            Assert.Equal(3, khongCoLyDo!.Count); // 1 null + 1 empty + 1 whitespace = 3

            Assert.NotNull(doiY);
            Assert.Equal(2, doiY!.Count);

            Assert.NotNull(saiThongTin);
            Assert.Equal(1, saiThongTin!.Count);
        }

        [Fact]
        public async Task GetOverviewAsync_DateRange_FiltersVietnamTime()
        {
            // Order.CreatedAt stores Vietnam time (UtcNow.AddHours(7)). The filter must compare
            // directly against Vietnam-time values, not shifted by timezone offset.
            var options = TestDbContextFactory.CreateSqliteOptions();
            using var context = new ApplicationDbContext(options);

            // Evening Vietnam time — old ConvertToUtcForQuery shifted end = '2026-06-03 16:59:59'
            // and excluded this row.
            context.Orders.AddRange(
                new Order { Id = 10, OrderNumber = "ORD-EVE", CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0), Status = OrderStatus.Cancelled },
                new Order { Id = 11, OrderNumber = "ORD-EAR", CreatedAt = new DateTime(2026, 6, 3, 2, 0, 0), Status = OrderStatus.Cancelled },
                new Order { Id = 12, OrderNumber = "ORD-NXT", CreatedAt = new DateTime(2026, 6, 4, 1, 0, 0), Status = OrderStatus.Pending }
            );
            await context.SaveChangesAsync();

            var service = new CancelledOrdersStatisticsService(new UnitOfWork(context));
            var start = new DateTime(2026, 6, 3, 0, 0, 0);
            var end = new DateTime(2026, 6, 3, 23, 59, 59);
            var result = await service.GetOverviewAsync(start, end);

            Assert.True(result.IsValid);
            Assert.Equal(2, result.Data.TotalOrders);
            Assert.Equal(2, result.Data.TotalCancelledOrders);
        }

        [Fact]
        public async Task GetTrendAsync_Daily_UsesStoredVietnamTimeBuckets()
        {
            // Order.CreatedAt is stored as Vietnam local time (UtcNow.AddHours(7)). Buckets must
            // not apply a second timezone shift, otherwise a 20:00 row spills into the next day.
            var options = TestDbContextFactory.CreateSqliteOptions();
            using var context = new ApplicationDbContext(options);

            context.Orders.AddRange(
                new Order { Id = 20, OrderNumber = "ORD-D1-C", CreatedAt = new DateTime(2026, 6, 3, 20, 0, 0), Status = OrderStatus.Cancelled },
                new Order { Id = 21, OrderNumber = "ORD-D1-P", CreatedAt = new DateTime(2026, 6, 3, 2, 0, 0), Status = OrderStatus.Pending },
                new Order { Id = 22, OrderNumber = "ORD-D2-C", CreatedAt = new DateTime(2026, 6, 4, 1, 0, 0), Status = OrderStatus.Cancelled }
            );
            await context.SaveChangesAsync();

            var service = new CancelledOrdersStatisticsService(new UnitOfWork(context));
            var result = await service.GetTrendAsync(
                TrendPeriod.Daily,
                new DateTime(2026, 6, 3),
                new DateTime(2026, 6, 4, 23, 59, 59));

            Assert.True(result.IsValid);
            Assert.Equal(new[] { "03/06", "04/06" }, result.Data.Labels);
            Assert.Equal(new[] { 1, 1 }, result.Data.CancelledData);
            Assert.Equal(new[] { 50m, 100m }, result.Data.CancellationRateData);
        }

        [Fact]
        public async Task GetReasonStatisticsAsync_WorksOnSqliteProvider()
        {
            // Run the same scenario against SQLite to confirm the implementation does not rely on
            // GroupBy translation for string.IsNullOrWhiteSpace.
            var options = TestDbContextFactory.CreateSqliteOptions();
            using var context = new ApplicationDbContext(options);

            context.Orders.AddRange(
                new Order { Id = 1, OrderNumber = "ORD-1", Status = OrderStatus.Cancelled, CancelReason = null, CreatedAt = DateTime.UtcNow },
                new Order { Id = 2, OrderNumber = "ORD-2", Status = OrderStatus.Cancelled, CancelReason = "  \t ", CreatedAt = DateTime.UtcNow },
                new Order { Id = 3, OrderNumber = "ORD-3", Status = OrderStatus.Cancelled, CancelReason = "Khách đổi ý", CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new CancelledOrdersStatisticsService(new UnitOfWork(context));
            var result = await service.GetReasonStatisticsAsync(null, null);

            Assert.True(result.IsValid);
            Assert.Equal(3, result.Data.TotalCancelledOrders);
            Assert.Single(result.Data.Reasons, r => r.Reason == "Không có lý do" && r.Count == 2);
            Assert.Single(result.Data.Reasons, r => r.Reason == "Khách đổi ý" && r.Count == 1);
        }
    }
}
