using System.Security.Claims;
using Fruitables.Attributes;
using Fruitables.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fruitables.Filters;

/// <summary>
/// Authorization filter that checks if user has required permissions
/// </summary>
public class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<RequirePermissionFilter> _logger;

    public RequirePermissionFilter(
        IRbacService rbacService,
        ILogger<RequirePermissionFilter> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if action or controller has RequirePermissionAttribute
        var permissionAttributes = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .ToList();

        // If no permission attributes, allow access
        if (!permissionAttributes.Any())
        {
            return;
        }

        // Check if user is authenticated
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Unauthenticated user attempted to access {Action}",
                context.ActionDescriptor.DisplayName);
            
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            _logger.LogError("User is authenticated but has no valid user ID claim");
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check each permission attribute (multiple attributes are combined with AND logic)
        foreach (var attribute in permissionAttributes)
        {
            bool hasPermission;

            if (attribute.Logic == PermissionLogic.Or)
            {
                // User needs at least one of the permissions
                hasPermission = await _rbacService.HasAnyPermissionAsync(userId, attribute.Permissions);
            }
            else // PermissionLogic.And
            {
                // User needs all of the permissions
                hasPermission = await _rbacService.HasAllPermissionsAsync(userId, attribute.Permissions);
            }

            if (!hasPermission)
            {
                var permissionsStr = string.Join(", ", attribute.Permissions);
                var logicStr = attribute.Logic == PermissionLogic.Or ? "any of" : "all of";
                
                _logger.LogWarning(
                    "User {UserId} denied access to {Action}. Required {Logic}: {Permissions}",
                    userId,
                    context.ActionDescriptor.DisplayName,
                    logicStr,
                    permissionsStr);

                // Log access denied to audit log
                await LogAccessDeniedAsync(userId, context.ActionDescriptor.DisplayName ?? "Unknown", permissionsStr);

                context.Result = new ForbidResult();
                return;
            }
        }

        // All permission checks passed, allow access
        _logger.LogDebug("User {UserId} granted access to {Action}", userId, context.ActionDescriptor.DisplayName);
    }

    /// <summary>
    /// Log access denied event to audit log
    /// </summary>
    private async Task LogAccessDeniedAsync(int userId, string action, string requiredPermissions)
    {
        try
        {
            // Note: We're logging this as a system action (adminId = userId)
            // In a real scenario, you might want to create a separate audit log table for access attempts
            // For now, we'll just log it using the logger
            _logger.LogWarning(
                "Access denied: User {UserId} attempted to access {Action} without required permissions: {Permissions}",
                userId,
                action,
                requiredPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log access denied event for user {UserId}", userId);
        }
    }
}
