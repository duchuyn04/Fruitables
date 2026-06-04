using Fruitables.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Fruitables.Tests;

/// <summary>
/// Phase 5: Role-Permission Assignment — assign/revoke single/batch, idempotency, cache.
/// NOTE: ApplicationDbContext seeds User Id=1 (Admin) and Id=2 (SuperAdmin).
///       All test-specific users use Id >= 1001.
/// </summary>
public class RbacRolePermissionTests
{
    // --- AssignPermissionToRoleAsync -----------------------------------------

    [Fact]
    public async Task AssignPermissionToRoleAsync_Success_CreatesMappingAndAudit()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignPermissionToRoleAsync(10, 1, adminId: 1001);

        Assert.True(await ctx.RolePermissions.AnyAsync(rp => rp.RoleId == 10 && rp.PermissionId == 1));

        var audit = await ctx.RbacAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Assign" && a.EntityType == "RolePermission");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_RoleNotFound_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignPermissionToRoleAsync(999, 1, adminId: 1001));
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_InactiveRole_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedInactiveRole(ctx, 10, "Disabled");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignPermissionToRoleAsync(10, 1, adminId: 1001));
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_PermissionNotFound_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignPermissionToRoleAsync(10, 999, adminId: 1001));
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_AlreadyAssigned_IsIdempotent()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        // Should not throw, should not duplicate
        await svc.AssignPermissionToRoleAsync(10, 1, adminId: 1001);

        Assert.Equal(1, await ctx.RolePermissions.CountAsync(rp => rp.RoleId == 10 && rp.PermissionId == 1));
        // No new audit log for idempotent call
        Assert.Empty(await ctx.RbacAuditLogs.ToListAsync());
    }

    // --- AssignPermissionsToRoleAsync (batch) --------------------------------
    
    [Fact]
    public async Task AssignPermissionsToRoleAsync_NullList_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await svc.AssignPermissionsToRoleAsync(10, null!, adminId: 1001);
        Assert.Empty(await ctx.RolePermissions.ToListAsync());
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_EmptyList_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await svc.AssignPermissionsToRoleAsync(10, new List<int>(), adminId: 1001);
        Assert.Empty(await ctx.RolePermissions.ToListAsync());
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_DuplicateIds_CreatesOnlyOneMapping()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignPermissionsToRoleAsync(10, new List<int> { 1, 1 }, adminId: 1001);

        Assert.Equal(1, await ctx.RolePermissions.CountAsync(rp => rp.RoleId == 10));
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_SkipsExistingMappings()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "products.create", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001); // already assigned
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignPermissionsToRoleAsync(10, new List<int> { 1, 2 }, adminId: 1001);

        Assert.Equal(2, await ctx.RolePermissions.CountAsync(rp => rp.RoleId == 10));
        // Only one audit log (for permission 2)
        Assert.Single(await ctx.RbacAuditLogs.ToListAsync());
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_MissingPermission_Throws_NoPartialWrite()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignPermissionsToRoleAsync(10, new List<int> { 1, 999 }, adminId: 1001));

        // No mappings should have been written
        Assert.Empty(await ctx.RolePermissions.ToListAsync());
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_InactiveRole_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedInactiveRole(ctx, 10, "Disabled");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignPermissionsToRoleAsync(10, new List<int> { 1 }, adminId: 1001));
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_CreatesAuditLogForEachNewMapping()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "products.create", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignPermissionsToRoleAsync(10, new List<int> { 1, 2 }, adminId: 1001);

        var auditCount = await ctx.RbacAuditLogs.CountAsync(a => a.Action == "Assign");
        Assert.Equal(2, auditCount);
    }

    // --- RevokePermissionFromRoleAsync ---------------------------------------

    [Fact]
    public async Task RevokePermissionFromRoleAsync_Success_RemovesMappingAndAudits()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.RevokePermissionFromRoleAsync(10, 1, adminId: 1001);

        Assert.Empty(await ctx.RolePermissions.ToListAsync());

        var audit = await ctx.RbacAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Revoke" && a.EntityType == "RolePermission");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task RevokePermissionFromRoleAsync_NotAssigned_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        // Should not throw
        await svc.RevokePermissionFromRoleAsync(10, 1, adminId: 1001);
        Assert.Empty(await ctx.RbacAuditLogs.ToListAsync());
    }

    // --- GetRolePermissionsAsync ---------------------------------------------

    [Fact]
    public async Task GetRolePermissionsAsync_ReturnsOrderedPermissions()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "orders.view",     "orders");
        RbacTestHelper.SeedPermission(ctx, 3, "products.create", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1);
        RbacTestHelper.SeedRolePermission(ctx, 2, 10, 2);
        RbacTestHelper.SeedRolePermission(ctx, 3, 10, 3);
        await ctx.SaveChangesAsync();

        var svc   = RbacTestHelper.CreateService(ctx);
        var perms = await svc.GetRolePermissionsAsync(10);

        Assert.Equal(3, perms.Count);
        // Ordered by module then name: orders.view, products.create, products.view
        Assert.Equal("orders.view",     perms[0].Name);
        Assert.Equal("products.create", perms[1].Name);
        Assert.Equal("products.view",   perms[2].Name);
    }

    // --- Cache invalidation via role permissions -----------------------------

    [Fact]
    public async Task AssignPermissionToRoleAsync_InvalidatesCacheForUsersWithRole()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "products.create", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc   = RbacTestHelper.CreateService(ctx, cache);

        // Populate cache with existing permissions
        var before = await svc.GetUserPermissionsAsync(1100);
        Assert.Single(before);

        // Assign second permission — should invalidate cache
        await svc.AssignPermissionToRoleAsync(10, 2, adminId: 1001);

        // Re-read should now return 2 permissions
        var after = await svc.GetUserPermissionsAsync(1100);
        Assert.Equal(2, after.Count);
    }

    [Fact]
    public async Task RevokePermissionFromRoleAsync_InvalidatesCacheForUsersWithRole()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = RbacTestHelper.CreateService(ctx, cache);

        // Populate cache
        var before = await svc.GetUserPermissionsAsync(1100);
        Assert.Single(before);

        // Revoke permission
        await svc.RevokePermissionFromRoleAsync(10, 1, adminId: 1001);

        // Re-read — cache should be invalidated and return empty
        var after = await svc.GetUserPermissionsAsync(1100);
        Assert.Empty(after);
    }
}
