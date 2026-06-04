using Fruitables.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fruitables.Tests;

/// <summary>
/// Phase 4: Permission Management — CRUD, format validation, and queries.
/// NOTE: ApplicationDbContext seeds User Id=1 (Admin) and Id=2 (SuperAdmin).
///       All test-specific users use Id >= 1001.
/// </summary>
public class RbacPermissionManagementTests
{
    // --- CreatePermissionAsync -----------------------------------------------

    [Fact]
    public async Task CreatePermissionAsync_Success_CreatesPermissionAndAudit()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();

        var svc  = RbacTestHelper.CreateService(ctx);
        var perm = await svc.CreatePermissionAsync("products.view", "products", "View products", adminId: 1001);

        Assert.NotNull(await ctx.Permissions.FindAsync(perm.Id));
        Assert.Equal("products", perm.Module);

        var audit = await ctx.RbacAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Create" && a.EntityType == "Permission");
        Assert.NotNull(audit);
        Assert.Contains("products.view", audit!.NewValue);
    }

    [Fact]
    public async Task CreatePermissionAsync_EmptyName_ThrowsArgumentException()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreatePermissionAsync("", "module", null, adminId: 1001));
    }

    [Theory]
    [InlineData("noDot")]           // no dot
    [InlineData("too.many.dots")]   // more than one dot
    [InlineData(".action")]         // empty module
    [InlineData("module.")]         // empty action
    public async Task CreatePermissionAsync_InvalidFormat_ThrowsArgumentException(string name)
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreatePermissionAsync(name, "module", null, adminId: 1001));
    }

    [Fact]
    public async Task CreatePermissionAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreatePermissionAsync("products.view", "products", null, adminId: 1001));
    }

    [Fact]
    public async Task CreatePermissionAsync_ModuleSavedCorrectly()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        await ctx.SaveChangesAsync();

        var svc  = RbacTestHelper.CreateService(ctx);
        var perm = await svc.CreatePermissionAsync("orders.view", "orders", null, adminId: 1001);

        Assert.Equal("orders", perm.Module);
    }

    // --- DeletePermissionAsync -----------------------------------------------

    [Fact]
    public async Task DeletePermissionAsync_UnassignedPermission_DeletesAndAudits()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedPermission(ctx, 1, "orders.view", "orders");
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);
        await svc.DeletePermissionAsync(1, adminId: 1001);

        Assert.Null(await ctx.Permissions.FindAsync(1));

        var audit = await ctx.RbacAuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Delete" && a.EntityType == "Permission");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task DeletePermissionAsync_NotFound_Throws()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeletePermissionAsync(999, adminId: 1001));
    }

    [Fact]
    public async Task DeletePermissionAsync_AssignedToRole_ThrowsAndDoesNotDelete()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedAdminUser(ctx, 1001);
        RbacTestHelper.SeedActiveRole(ctx, 10, "Editor");
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        RbacTestHelper.SeedRolePermission(ctx, 1, 10, 1, assignedByAdminId: 1001);
        await ctx.SaveChangesAsync();

        var svc = RbacTestHelper.CreateService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeletePermissionAsync(1, adminId: 1001));

        Assert.NotNull(await ctx.Permissions.FindAsync(1));
    }

    // --- GetPermissionByIdAsync ----------------------------------------------

    [Fact]
    public async Task GetPermissionByIdAsync_Found_ReturnsPermission()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedPermission(ctx, 1, "products.view", "products");
        await ctx.SaveChangesAsync();

        var svc  = RbacTestHelper.CreateService(ctx);
        var perm = await svc.GetPermissionByIdAsync(1);

        Assert.NotNull(perm);
        Assert.Equal("products.view", perm!.Name);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_NotFound_ReturnsNull()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        await ctx.SaveChangesAsync();

        var svc  = RbacTestHelper.CreateService(ctx);
        var perm = await svc.GetPermissionByIdAsync(999);

        Assert.Null(perm);
    }

    // --- GetAllPermissionsAsync ----------------------------------------------

    [Fact]
    public async Task GetAllPermissionsAsync_OrdersByModuleThenName()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "orders.view",     "orders");
        RbacTestHelper.SeedPermission(ctx, 3, "products.create", "products");
        await ctx.SaveChangesAsync();

        var svc   = RbacTestHelper.CreateService(ctx);
        var perms = await svc.GetAllPermissionsAsync();

        // orders.view, products.create, products.view
        Assert.Equal("orders",   perms[0].Module);
        Assert.Equal("products", perms[1].Module);
        Assert.Equal("products.create", perms[1].Name);
        Assert.Equal("products.view",   perms[2].Name);
    }

    // --- GetPermissionsByModuleAsync -----------------------------------------

    [Fact]
    public async Task GetPermissionsByModuleAsync_ReturnsOnlyRequestedModule()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "orders.view",     "orders");
        RbacTestHelper.SeedPermission(ctx, 3, "products.create", "products");
        await ctx.SaveChangesAsync();

        var svc   = RbacTestHelper.CreateService(ctx);
        var perms = await svc.GetPermissionsByModuleAsync("products");

        Assert.Equal(2, perms.Count);
        Assert.All(perms, p => Assert.Equal("products", p.Module));
        // Ordered by name
        Assert.Equal("products.create", perms[0].Name);
        Assert.Equal("products.view",   perms[1].Name);
    }

    // --- GetPermissionsGroupedByModuleAsync ----------------------------------

    [Fact]
    public async Task GetPermissionsGroupedByModuleAsync_GroupsCorrectly()
    {
        using var ctx = RbacTestHelper.CreateSqliteContext();
        RbacTestHelper.SeedPermission(ctx, 1, "products.view",   "products");
        RbacTestHelper.SeedPermission(ctx, 2, "orders.view",     "orders");
        RbacTestHelper.SeedPermission(ctx, 3, "products.create", "products");
        await ctx.SaveChangesAsync();

        var svc     = RbacTestHelper.CreateService(ctx);
        var grouped = await svc.GetPermissionsGroupedByModuleAsync();

        Assert.True(grouped.ContainsKey("products"));
        Assert.True(grouped.ContainsKey("orders"));
        Assert.Equal(2, grouped["products"].Count);
        Assert.Single(grouped["orders"]);
    }
}
