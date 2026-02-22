using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fruitables.Services;

/// <summary>
/// Implementation of Migration Service for RBAC system
/// Handles migration from legacy UserRole enum to RBAC system
/// </summary>
public class MigrationService : IMigrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRbacService _rbacService;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IUnitOfWork unitOfWork,
        IRbacService rbacService,
        ILogger<MigrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _rbacService = rbacService;
        _logger = logger;
    }

    // ==================== Các thao tác migration ====================
    
    public async Task<MigrationResult> MigrateToRbacAsync()
    {
        var result = new MigrationResult
        {
            CompletedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting RBAC migration...");

            // Get all users from database with their role mappings
            var users = await _unitOfWork.Users
                .Query()
                .Include(u => u.UserRoleMappings)
                .ToListAsync();

            _logger.LogInformation("Found {UserCount} users to migrate", users.Count);

            int usersProcessed = 0;
            var errors = new List<string>();

            foreach (var user in users)
            {
                try
                {
                    // Check if user already has RBAC role mapping
                    if (user.UserRoleMappings.Any())
                    {
                        _logger.LogDebug("User {UserId} already has RBAC role mappings, skipping", user.Id);
                        continue;
                    }

                    // Get the role based on legacy UserRole enum
                    var roleName = user.Role.ToString();
                    var roles = await _unitOfWork.Roles
                        .FindAsync(r => r.Name == roleName && r.IsActive);
                    
                    var role = roles.FirstOrDefault();
                    
                    if (role == null)
                    {
                        var error = $"Role '{roleName}' not found for user {user.Id}";
                        _logger.LogWarning(error);
                        errors.Add(error);
                        continue;
                    }

                    // Create UserRoleMapping
                    var userRoleMapping = new UserRoleMapping
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow,
                        AssignedByAdminId = null // System migration
                    };

                    await _unitOfWork.UserRoleMappings.AddAsync(userRoleMapping);
                    usersProcessed++;

                    _logger.LogDebug("Migrated user {UserId} to role {RoleName}", user.Id, roleName);
                }
                catch (Exception ex)
                {
                    var error = $"Error migrating user {user.Id}: {ex.Message}";
                    _logger.LogError(ex, error);
                    errors.Add(error);
                }
            }

            // Save all changes
            await _unitOfWork.SaveChangesAsync();

            result.Success = errors.Count == 0;
            result.UsersProcessed = usersProcessed;
            result.Errors = errors;

            _logger.LogInformation(
                "RBAC migration completed. Users processed: {UsersProcessed}, Errors: {ErrorCount}",
                usersProcessed,
                errors.Count
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during RBAC migration");
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            return result;
        }
    }
    
    public async Task<MigrationResult> RollbackToLegacyAsync()
    {
        var result = new MigrationResult
        {
            CompletedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting RBAC rollback...");

            // Get all UserRole mappings
            var userRoleMappings = await _unitOfWork.UserRoleMappings
                .Query()
                .ToListAsync();

            _logger.LogInformation("Found {MappingCount} user role mappings to remove", userRoleMappings.Count);

            // Remove all UserRole mappings
            _unitOfWork.UserRoleMappings.RemoveRange(userRoleMappings);
            
            // Save changes
            await _unitOfWork.SaveChangesAsync();

            result.Success = true;
            result.UsersProcessed = userRoleMappings.Select(urm => urm.UserId).Distinct().Count();

            _logger.LogInformation(
                "RBAC rollback completed. Removed {MappingCount} mappings affecting {UserCount} users",
                userRoleMappings.Count,
                result.UsersProcessed
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during RBAC rollback");
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            return result;
        }
    }
    
    public async Task<MigrationStatus> GetMigrationStatusAsync()
    {
        try
        {
            // Get total number of users
            var totalUsers = await _unitOfWork.Users.CountAsync();
            
            // Get number of users with RBAC role mappings
            var migratedUsers = await _unitOfWork.UserRoleMappings
                .Query()
                .Select(urm => urm.UserId)
                .Distinct()
                .CountAsync();
            
            // Get last migration date (most recent UserRoleMapping creation)
            var lastMigrationDate = await _unitOfWork.UserRoleMappings
                .Query()
                .OrderByDescending(urm => urm.AssignedAt)
                .Select(urm => (DateTime?)urm.AssignedAt)
                .FirstOrDefaultAsync();
            
            return new MigrationStatus
            {
                IsCompleted = totalUsers > 0 && migratedUsers == totalUsers,
                TotalUsers = totalUsers,
                MigratedUsers = migratedUsers,
                LastMigrationDate = lastMigrationDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting migration status");
            return new MigrationStatus
            {
                IsCompleted = false,
                TotalUsers = 0,
                MigratedUsers = 0,
                LastMigrationDate = null
            };
        }
    }
    
    // ==================== Các thao tác seed ====================
    
    public async Task SeedDefaultRolesAsync()
    {
        try
        {
            _logger.LogInformation("Seeding default roles...");

            var defaultRoles = new[]
            {
                new Role { Name = "Customer", Description = "Customer role with basic permissions", IsActive = true },
                new Role { Name = "Admin", Description = "Admin role with most permissions", IsActive = true },
                new Role { Name = "SuperAdmin", Description = "SuperAdmin role with all permissions", IsActive = true }
            };

            foreach (var role in defaultRoles)
            {
                var existingRole = await _unitOfWork.Roles
                    .FirstOrDefaultAsync(r => r.Name == role.Name);
                
                if (existingRole == null)
                {
                    await _unitOfWork.Roles.AddAsync(role);
                    _logger.LogInformation("Created role: {RoleName}", role.Name);
                }
                else
                {
                    _logger.LogDebug("Role already exists: {RoleName}", role.Name);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Default roles seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default roles");
            throw;
        }
    }
    
    public async Task SeedDefaultPermissionsAsync()
    {
        try
        {
            _logger.LogInformation("Seeding default permissions...");

            var defaultPermissions = new[]
            {
                // Products module
                new Permission { Name = "products.view", Description = "View products", Module = "products" },
                new Permission { Name = "products.create", Description = "Create products", Module = "products" },
                new Permission { Name = "products.update", Description = "Update products", Module = "products" },
                new Permission { Name = "products.delete", Description = "Delete products", Module = "products" },
                new Permission { Name = "products.manage_inventory", Description = "Manage product inventory", Module = "products" },
                
                // Orders module
                new Permission { Name = "orders.view_all", Description = "View all orders", Module = "orders" },
                new Permission { Name = "orders.view_own", Description = "View own orders", Module = "orders" },
                new Permission { Name = "orders.create", Description = "Create orders", Module = "orders" },
                new Permission { Name = "orders.update_status", Description = "Update order status", Module = "orders" },
                new Permission { Name = "orders.cancel", Description = "Cancel orders", Module = "orders" },
                new Permission { Name = "orders.refund", Description = "Process refunds", Module = "orders" },
                
                // Users module
                new Permission { Name = "users.view", Description = "View users", Module = "users" },
                new Permission { Name = "users.create", Description = "Create users", Module = "users" },
                new Permission { Name = "users.update", Description = "Update users", Module = "users" },
                new Permission { Name = "users.lock", Description = "Lock user accounts", Module = "users" },
                new Permission { Name = "users.unlock", Description = "Unlock user accounts", Module = "users" },
                new Permission { Name = "users.delete", Description = "Delete users", Module = "users" },
                
                // Reviews module
                new Permission { Name = "reviews.view", Description = "View reviews", Module = "reviews" },
                new Permission { Name = "reviews.create", Description = "Create reviews", Module = "reviews" },
                new Permission { Name = "reviews.edit_own", Description = "Edit own reviews", Module = "reviews" },
                new Permission { Name = "reviews.delete_own", Description = "Delete own reviews", Module = "reviews" },
                new Permission { Name = "reviews.moderate", Description = "Moderate reviews", Module = "reviews" },
                new Permission { Name = "reviews.delete", Description = "Delete reviews", Module = "reviews" },
                new Permission { Name = "reviews.view_reports", Description = "View review reports", Module = "reviews" },
                new Permission { Name = "reviews.view_statistics", Description = "View review statistics", Module = "reviews" },
                
                // Settings module
                new Permission { Name = "settings.view", Description = "View settings", Module = "settings" },
                new Permission { Name = "settings.update", Description = "Update settings", Module = "settings" },
                
                // Dashboard module
                new Permission { Name = "dashboard.view", Description = "View dashboard", Module = "dashboard" },
                new Permission { Name = "dashboard.view_statistics", Description = "View detailed statistics", Module = "dashboard" },
                
                // System module
                new Permission { Name = "system.manage", Description = "Manage entire system", Module = "system" },
                new Permission { Name = "system.view_logs", Description = "View system logs", Module = "system" },
                new Permission { Name = "system.manage_rbac", Description = "Manage roles and permissions", Module = "system" }
            };

            foreach (var permission in defaultPermissions)
            {
                var existingPermission = await _unitOfWork.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name);
                
                if (existingPermission == null)
                {
                    await _unitOfWork.Permissions.AddAsync(permission);
                    _logger.LogDebug("Created permission: {PermissionName}", permission.Name);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Default permissions seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default permissions");
            throw;
        }
    }
    
    public async Task SeedRolePermissionMappingsAsync()
    {
        try
        {
            _logger.LogInformation("Seeding role-permission mappings...");

            // Get roles
            var customerRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            var adminRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var superAdminRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

            if (customerRole == null || adminRole == null || superAdminRole == null)
            {
                throw new InvalidOperationException("Default roles must be seeded before role-permission mappings");
            }

            // Get all permissions
            var allPermissions = await _unitOfWork.Permissions.GetAllAsync();
            var permissionDict = allPermissions.ToDictionary(p => p.Name, p => p);

            // Customer permissions
            var customerPermissions = new[]
            {
                "products.view",
                "orders.view_own",
                "orders.create",
                "reviews.view",
                "reviews.create",
                "reviews.edit_own",
                "reviews.delete_own"
            };

            await AssignPermissionsToRole(customerRole.Id, customerPermissions, permissionDict);

            // Admin permissions (all except system.manage)
            var adminPermissions = allPermissions
                .Where(p => p.Name != "system.manage")
                .Select(p => p.Name)
                .ToArray();

            await AssignPermissionsToRole(adminRole.Id, adminPermissions, permissionDict);

            // SuperAdmin permissions (all)
            var superAdminPermissions = allPermissions.Select(p => p.Name).ToArray();
            await AssignPermissionsToRole(superAdminRole.Id, superAdminPermissions, permissionDict);

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Role-permission mappings seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding role-permission mappings");
            throw;
        }
    }

    private async Task AssignPermissionsToRole(int roleId, string[] permissionNames, Dictionary<string, Permission> permissionDict)
    {
        foreach (var permissionName in permissionNames)
        {
            if (!permissionDict.TryGetValue(permissionName, out var permission))
            {
                _logger.LogWarning("Permission not found: {PermissionName}", permissionName);
                continue;
            }

            var existingMapping = await _unitOfWork.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);

            if (existingMapping == null)
            {
                await _unitOfWork.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }
    }
    
    public async Task ResetToDefaultSeedDataAsync()
    {
        try
        {
            _logger.LogInformation("Resetting to default seed data...");

            // Remove all existing role-permission mappings
            var existingMappings = await _unitOfWork.RolePermissions.GetAllAsync();
            _unitOfWork.RolePermissions.RemoveRange(existingMappings);

            // Remove all existing permissions
            var existingPermissions = await _unitOfWork.Permissions.GetAllAsync();
            _unitOfWork.Permissions.RemoveRange(existingPermissions);

            // Remove all existing roles
            var existingRoles = await _unitOfWork.Roles.GetAllAsync();
            _unitOfWork.Roles.RemoveRange(existingRoles);

            await _unitOfWork.SaveChangesAsync();

            // Re-seed everything
            await SeedDefaultRolesAsync();
            await SeedDefaultPermissionsAsync();
            await SeedRolePermissionMappingsAsync();

            _logger.LogInformation("Reset to default seed data completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting to default seed data");
            throw;
        }
    }
}
