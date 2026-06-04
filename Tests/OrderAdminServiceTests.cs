using Xunit;
using Moq;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Services;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using System.Reflection;

namespace Fruitables.Tests
{
    public class OrderAdminServiceTests
    {
        [Fact]
        public async Task UpdateOrderStatusAsync_CancelOrder_AtomicStatusStockAndHistory()
        {
            // Arrange
            var options = TestDbContextFactory.CreateSqliteOptions();
            using var context = new ApplicationDbContext(options);

            context.Users.Add(new User
            {
                Id = 100,
                Name = "Test Admin",
                Email = "admin@example.com",
                Password = "hashed",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.Categories.Add(new Category { Id = 1, Name = "Default", Slug = "default" });

            var product = new Product
            {
                Id = 1,
                CategoryId = 1,
                Name = "Apple",
                Slug = "apple",
                Price = 10,
                StockQuantity = 5,
                MinOrderQuantity = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var order = new Order
            {
                Id = 10,
                OrderNumber = "ORD-12345",
                Status = OrderStatus.Processing,
                Subtotal = 20,
                Total = 20,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, ProductName = "Apple", Quantity = 2, Price = 10, Total = 20 }
                }
            };

            context.Products.Add(product);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var logServiceMock = new Mock<IOrderLogService>();
            var notifierMock = new Mock<IRealtimeNotifier>();
            var adminService = new OrderAdminService(context, logServiceMock.Object, notifierMock.Object);

            // Act
            var result = await adminService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
            {
                OrderId = 10,
                NewStatus = OrderStatus.Cancelled,
                AdminId = 100,
                Notes = "Customer cancelled"
            });

            // Assert
            Assert.True(result.Success);
            Assert.Equal(OrderStatus.Cancelled, order.Status);

            // Stock restored atomically (5 + 2 = 7).
            var updatedProduct = await context.Products.FindAsync(1);
            Assert.NotNull(updatedProduct);
            Assert.Equal(7, updatedProduct.StockQuantity);

            // History row was created inline (no separate log service save).
            var history = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(context.OrderStatusHistories, h => h.OrderId == 10);
            Assert.NotNull(history);
            Assert.Equal(OrderStatus.Processing, history!.OldStatus);
            Assert.Equal(OrderStatus.Cancelled, history.NewStatus);

            // Log service is no longer required for status change (kept only for payment status log).
            logServiceMock.Verify(
                s => s.LogStatusChangeAsync(It.IsAny<int>(), It.IsAny<OrderStatus>(), It.IsAny<OrderStatus>(), It.IsAny<int>(), It.IsAny<string?>()),
                Times.Never);

            notifierMock.Verify(n => n.NotifyOrderUpdatedAsync(10, order.UserId, "Cancelled"), Times.Once);
            notifierMock.Verify(n => n.NotifyStockChangedAsync(1, 7), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_RestoreFromCancelled_WithInsufficientStock_RollsBack()
        {
            // Arrange
            var options = TestDbContextFactory.CreateSqliteOptions();
            using var context = new ApplicationDbContext(options);

            context.Users.Add(new User
            {
                Id = 100,
                Name = "Test Admin",
                Email = "admin@example.com",
                Password = "hashed",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.Categories.Add(new Category { Id = 1, Name = "Default", Slug = "default" });

            var product = new Product
            {
                Id = 1,
                CategoryId = 1,
                Name = "Apple",
                Slug = "apple",
                Price = 10,
                StockQuantity = 2,
                MinOrderQuantity = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var order = new Order
            {
                Id = 11,
                OrderNumber = "ORD-22222",
                Status = OrderStatus.Cancelled,
                Subtotal = 40,
                Total = 40,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, ProductName = "Apple", Quantity = 5, Price = 10, Total = 50 }
                }
            };

            context.Products.Add(product);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var logServiceMock = new Mock<IOrderLogService>();
            var notifierMock = new Mock<IRealtimeNotifier>();
            var adminService = new OrderAdminService(context, logServiceMock.Object, notifierMock.Object);

            // Act
            var result = await adminService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
            {
                OrderId = 11,
                NewStatus = OrderStatus.Processing,
                AdminId = 100,
                Notes = "Restoring"
            });

            // Assert
            Assert.False(result.Success);
            Assert.Equal(OrderErrorType.InsufficientStock, result.ErrorType);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal(2, (await context.Products.FindAsync(1))!.StockQuantity);

            notifierMock.Verify(n => n.NotifyOrderUpdatedAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
            notifierMock.Verify(n => n.NotifyStockChangedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
