using Xunit;
using Microsoft.EntityFrameworkCore;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Repositories;

namespace Fruitables.Tests;

// Guardrail: CountingQueryInterceptor phải đếm cả scalar query (SELECT COUNT, EXISTS).
// Hiện tại chỉ override ReaderExecuting nên CountAsync/AnyAsync bỏ sót.
public class CountingQueryInterceptorScalarTests
{
    [Fact]
    public async Task CountAsync_OnRegisteredTable_IncrementsGetCount()
    {
        var interceptor = new CountingQueryInterceptor();
        var options = TestDbContextFactory.CreateSqliteOptions(interceptor);
        interceptor.Register("Orders");

        using var context = new ApplicationDbContext(options);
        context.Orders.Add(new Order
        {
            Id = 1,
            OrderNumber = "ORD-1",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Reset counts — scalar query from SaveChanges is not relevant.
        var before = interceptor.GetCount("Orders");

        // CountAsync gửi SELECT COUNT(*) — phải được đếm.
        var count = await context.Orders.CountAsync();

        Assert.Equal(1, count);
        Assert.Equal(1, interceptor.GetCount("Orders") - before);
    }

    [Fact]
    public async Task AnyAsync_OnRegisteredTable_IncrementsGetCount()
    {
        var interceptor = new CountingQueryInterceptor();
        var options = TestDbContextFactory.CreateSqliteOptions(interceptor);
        interceptor.Register("Orders");

        using var context = new ApplicationDbContext(options);
        context.Orders.Add(new Order
        {
            Id = 2,
            OrderNumber = "ORD-2",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var before = interceptor.GetCount("Orders");
        var exists = await context.Orders.AnyAsync();

        Assert.True(exists);
        Assert.Equal(1, interceptor.GetCount("Orders") - before);
    }

    [Fact]
    public async Task ProductSelectCount_StillWorksWithReaderQueries()
    {
        // ProductSelectCount từ interceptor cũ không bị ảnh hưởng bởi scalar overrides.
        var interceptor = new CountingQueryInterceptor();
        var options = TestDbContextFactory.CreateSqliteOptions(interceptor);

        using var context = new ApplicationDbContext(options);
        context.Categories.Add(new Category { Id = 1, Name = "Default", Slug = "default" });
        context.Products.Add(new Product
        {
            Id = 1, CategoryId = 1, Name = "Test", Slug = "test",
            Price = 10, StockQuantity = 10, MinOrderQuantity = 1,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var before = interceptor.ProductSelectCount;
        context.ChangeTracker.Clear();
        var product = await context.Products.FindAsync(1);

        Assert.NotNull(product);
        Assert.Equal(1, interceptor.ProductSelectCount - before);
    }
}
