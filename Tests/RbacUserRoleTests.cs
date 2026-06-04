using Fruitables.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fruitables.Tests;

/// <summary>
/// Phase 6: User-Role Assignment — assign, revoke, legacy sync, idempotency, cache.
/// All tests use SQLite to validate transactional behavior.
/// NOTE: ApplicationDbContext seeds User Id=1 (Admin) and Id=2 (SuperAdmin).
///       All test-specific users use Id >= 1001.
/// </summary>
public class RbacUserRoleTests
{
    // --- AssignRolesToUserAsync - validation ---------------------------------
    
    [Fact]
    public async Task AssignRolesToUserAsync_NullList_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, null!, adminId: 1001);

        Assert.Empty(await ctx.UserRoleMappings.ToListAsync());
    }

    [Fact]
    public async Task AssignRolesToUserAsync_EmptyList_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int>(), adminId: 1001);

        Assert.Empty(await ctx.UserRoleMappings.ToListAsync());
    }

    [Fact]
    public async Task AssignRolesToUserAsync_MoreThanOneUniqueRole_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRolesToUserAsync(1100, new List<int> { 1, 2 }, adminId: 1001));
    }

    [Fact]
    public async Task AssignRolesToUserAsync_UserNotFound_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRolesToUserAsync(9999, new List<int> { 10 }, adminId: 1001));
    }

    [Fact]
    public async Task AssignRolesToUserAsync_RoleNotFound_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRolesToUserAsync(1100, new List<int> { 999 }, adminId: 1001));
    }

    [Fact]
    public async Task AssignRolesToUserAsync_InactiveRole_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedInactiveRole(ctx, 10, "Disabled");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AssignRolesToUserAsync(1100, new List<int> { 10 }, adminId: 1001));
    }

    // --- AssignRolesToUserAsync - success / idempotency ----------------------

    [Fact]
    public async Task AssignRolesToUserAsync_SameExistingRole_IsNoOp_NoAdditionalAudit()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int> { 10 }, adminId: 1001);

        // Still only one mapping
        Assert.Single(await ctx.UserRoleMappings.Where(m => m.UserId == 1100).ToListAsync());
        // No audit logs created
        Assert.Empty(await ctx.RbacAuditLogs.ToListAsync());
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReplacesOldRole_AddsNewRole_AuditsRevokeAndAssign()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "OldRole");
        RbacTestHelper.SeedActiveRole(ctx, 20, "Admin");
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int> { 20 }, adminId: 1001);

        var mappings = await ctx.UserRoleMappings.Where(m => m.UserId == 1100).ToListAsync();
        Assert.Single(mappings);
        Assert.Equal(20, mappings[0].RoleId);

        var logs = await ctx.RbacAuditLogs.ToListAsync();
        Assert.Contains(logs, l => l.Action == "Revoke");
        Assert.Contains(logs, l => l.Action == "Assign");
    }

    // --- Legacy role sync ----------------------------------------------------

    [Fact]
    public async Task AssignRolesToUserAsync_SyncsLegacyRole_Admin()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Admin");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int> { 10 }, adminId: 1001);

        var user = await ctx.Users.FindAsync(1100);
        Assert.Equal(UserRole.Admin, user!.Role);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_SyncsLegacyRole_SuperAdmin()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "SuperAdmin");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int> { 10 }, adminId: 1001);

        var user = await ctx.Users.FindAsync(1100);
        Assert.Equal(UserRole.SuperAdmin, user!.Role);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_SyncsLegacyRole_DefaultCustomer()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        var user = RbacTestHelper.SeedCustomerUser(ctx, 1100);
        // Pre-set user.Role to Admin to verify it gets reset to Customer
        user.Role = UserRole.Admin;
        RbacTestHelper.SeedActiveRole(ctx, 10, "SomeOtherRole");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRolesToUserAsync(1100, new List<int> { 10 }, adminId: 1001);

        var updatedUser = await ctx.Users.FindAsync(1100);
        Assert.Equal(UserRole.Customer, updatedUser!.Role);
    }

    // --- AssignRoleToUserAsync (single delegate) -----------------------------

    [Fact]
    public async Task AssignRoleToUserAsync_DelegatesToTransactionalPath()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Admin");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.AssignRoleToUserAsync(1100, 10, adminId: 1001);

        var user = await ctx.Users.FindAsync(1100);
        Assert.Equal(UserRole.Admin, user!.Role);
        Assert.Single(await ctx.UserRoleMappings.Where(m => m.UserId == 1100).ToListAsync());
    }

    // --- RevokeRoleFromUserAsync ---------------------------------------------

    [Fact]
    public async Task RevokeRoleFromUserAsync_Success_RemovesMappingAndAuditAndSyncsLegacy()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        var user = RbacTestHelper.SeedCustomerUser(ctx, 1100);
        user.Role = UserRole.Admin;
        RbacTestHelper.SeedActiveRole(ctx, 10, "Admin");
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.RevokeRoleFromUserAsync(1100, 10, adminId: 1001);

        Assert.Empty(await ctx.UserRoleMappings.Where(m => m.UserId == 1100).ToListAsync());

        var audit = await ctx.RbacAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Revoke" && a.EntityType == "UserRole");
        Assert.NotNull(audit);

        // Legacy role should fall back to Customer
        var updatedUser = await ctx.Users.FindAsync(1100);
        Assert.Equal(UserRole.Customer, updatedUser!.Role);
    }

    [Fact]
    public async Task RevokeRoleFromUserAsync_NotAssigned_IsNoOp()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        // Should not throw
        await svc.RevokeRoleFromUserAsync(1100, 10, adminId: 1001);
        Assert.Empty(await ctx.RbacAuditLogs.ToListAsync());
    }

    // --- GetUserRolesAsync ---------------------------------------------------

    [Fact]
    public async Task GetUserRolesAsync_ReturnsRolesOrderedByName()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Zulu");
        RbacTestHelper.SeedActiveRole(ctx, 11, "Alpha");
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        RbacTestHelper.SeedUserRoleMapping(ctx, 2, 1100, 11, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc   = RbacTestHelper.CreateService(ctx);
        var roles = await svc.GetUserRolesAsync(1100);

        Assert.Equal(2, roles.Count);
        Assert.Equal("Alpha", roles[0].Name);
        Assert.Equal("Zulu",  roles[1].Name);
    }

    // --- Cache invalidation --------------------------------------------------

    [Fact]
    public async Task AssignRolesToUserAsync_InvalidatesUserCacheAfterChange()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedCustomerUser(ctx, 1100);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedActiveRole(ctx, 20, "Manager");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedPermission(ctx, 2, "orders.view",   "orders");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        RbacTestHelper.SeedRolePermission(ctx, 2, 20, 2, assignedByAdminId: 1001);
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var svc = RbacTestHelper.CreateService(ctx, cache);

        // Populate cache with Editor permissions
        var before = await svc.GetUserPermissionsAsync(1100);
        Assert.Contains("products.view", before);

        // Switch role to Manager
        await svc.AssignRolesToUserAsync(1100, new List<int> { 20 }, adminId: 1001);

        // Re-read — cache was invalidated, DB now returns Manager permissions
        var after = await svc.GetUserPermissionsAsync(1100);
        Assert.Contains("orders.view", after);
        Assert.DoesNotContain("products.view", after);
    }

    [Fact]
    public async Task RevokeRoleFromUserAsync_InvalidatesUserCacheAfterChange()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        var user = RbacTestHelper.SeedCustomerUser(ctx, 1100);
        user.Role = UserRole.Admin;
        RbacTestHelper.SeedActiveRole(ctx, 10, "Admin");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        RbacTestHelper.SeedUserRoleMapping(ctx, 1, 1100, 10, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var svc = RbacTestHelper.CreateService(ctx, cache);

        // Populate cache
        var before = await svc.GetUserPermissionsAsync(1100);
        Assert.Contains("products.view", before);

        // Revoke role
        await svc.RevokeRoleFromUserAsync(1100, 10, adminId: 1001);

        // Re-read — cache should be invalidated and return empty
        var after = await svc.GetUserPermissionsAsync(1100);
        Assert.Empty(after);
    }
}
