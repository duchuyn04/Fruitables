using Fruitables.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fruitables.Tests;

/// <summary>
/// Phase 7: Audit Log Querying — pagination, filters, sorting, navigation property.
/// NOTE: ApplicationDbContext seeds User Id=1 (Admin) and Id=2 (SuperAdmin).
///       All test-specific users use Id >= 1001.
/// </summary>
public class RbacAuditAndCacheTests
{
    /// <summary>Seeds N audit log rows with deterministic ChangedAt timestamps.</summary>
    private static async Task SeedAuditLogs(
        Fruitables.Data.ApplicationDbContext ctx,
        int adminId,
        int count,
        string entityType = "Role")
    {
        var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 1; i <= count; i++)
        {
            ctx.RbacAuditLogs.Add(new RbacAuditLog
            {
                Action           = "Create",
                EntityType       = entityType,
                EntityId         = i,
                ChangedByAdminId = adminId,
                ChangedAt        = baseTime.AddMinutes(i),
                OldValue         = null,
                NewValue         = $"{{\"Id\":{i}}}"
            });
        }
        await ctx.SaveChangesAsync();
    }

    // --- Pagination ----------------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsCorrectPageAndTotal()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 7);

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, total) = await svc.GetAuditLogsAsync(page: 2, pageSize: 3);

        Assert.Equal(7, total);
        Assert.Equal(3, logs.Count);

        // Newest first: IDs are 7, 6, 5, 4, 3, 2, 1
        // Page 1 (size 3): 7, 6, 5
        // Page 2 (size 3): 4, 3, 2
        Assert.Equal(4, logs[0].EntityId);
        Assert.Equal(3, logs[1].EntityId);
        Assert.Equal(2, logs[2].EntityId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_TotalCountIndependentFromPageSize()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 10);

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, total) = await svc.GetAuditLogsAsync(page: 1, pageSize: 2);

        Assert.Equal(10, total);
        Assert.Equal(2, logs.Count);
    }

    // --- Sorting -------------------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_SortedNewestFirst()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 5);

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, _) = await svc.GetAuditLogsAsync(page: 1, pageSize: 5);

        for (int i = 0; i < logs.Count - 1; i++)
            Assert.True(logs[i].ChangedAt >= logs[i + 1].ChangedAt);
    }

    // --- Filter by entityType ------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByEntityType()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 3, entityType: "Role");
        await SeedAuditLogs(ctx, adminId: 1001, count: 2, entityType: "Permission");

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, total) = await svc.GetAuditLogsAsync(1, 10, entityType: "Permission");

        Assert.Equal(2, total);
        Assert.All(logs, l => Assert.Equal("Permission", l.EntityType));
    }

    // --- Filter by adminId ---------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByAdminId()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedAdminUser(ctx, 1002);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 3);
        await SeedAuditLogs(ctx, adminId: 1002, count: 2);

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, total) = await svc.GetAuditLogsAsync(1, 10, changedByAdminId: 1002);

        Assert.Equal(2, total);
        Assert.All(logs, l => Assert.Equal(1002, l.ChangedByAdminId));
    }

    // --- Navigation property -------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_IncludesChangedByAdmin()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        var admin = RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();
        await SeedAuditLogs(ctx, adminId: 1001, count: 1);

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, _) = await svc.GetAuditLogsAsync(1, 10);

        Assert.Single(logs);
        Assert.NotNull(logs[0].ChangedByAdmin);
        Assert.Equal(admin.Name, logs[0].ChangedByAdmin.Name);
    }

    // --- Combined filter -----------------------------------------------------

    [Fact]
    public async Task GetAuditLogsAsync_EntityTypeAndAdminFilterCombined()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedAdminUser(ctx, 1002);
        await ctx.SaveChangesAsync();

        // Admin 1001: 2 Role logs
        await SeedAuditLogs(ctx, adminId: 1001, count: 2, entityType: "Role");
        // Admin 1002: 1 Role log + 1 Permission log
        await SeedAuditLogs(ctx, adminId: 1002, count: 1, entityType: "Role");
        await SeedAuditLogs(ctx, adminId: 1002, count: 1, entityType: "Permission");

        var svc = RbacTestHelper.CreateService(ctx);
        var (logs, total) = await svc.GetAuditLogsAsync(
            1, 10, entityType: "Role", changedByAdminId: 1002);

        Assert.Equal(1, total);
        Assert.All(logs, l =>
        {
            Assert.Equal("Role", l.EntityType);
            Assert.Equal(1002, l.ChangedByAdminId);
        });
    }
}
