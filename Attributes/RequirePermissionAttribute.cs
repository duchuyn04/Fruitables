using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fruitables.Attributes;

/// <summary>
/// Permission logic for combining multiple permissions
/// </summary>
public enum PermissionLogic
{
    /// <summary>
    /// User needs at least one of the specified permissions (OR logic)
    /// </summary>
    Or,
    
    /// <summary>
    /// User needs all of the specified permissions (AND logic)
    /// </summary>
    And
}

/// <summary>
/// Authorization attribute that requires specific permissions to access a controller or action
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Array of required permissions
    /// </summary>
    public string[] Permissions { get; }
    
    /// <summary>
    /// Logic for combining multiple permissions (OR or AND)
    /// Default is OR - user needs at least one permission
    /// </summary>
    public PermissionLogic Logic { get; set; } = PermissionLogic.Or;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="permissions">Required permissions (format: module.action)</param>
    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        
        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required", nameof(permissions));
        }
    }
    
    /// <summary>
    /// Called early in the filter pipeline to confirm request is authorized
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // The actual authorization logic is implemented in RequirePermissionFilter
        // This attribute just holds the configuration
        // The filter will be registered globally and will check for this attribute
    }
}
