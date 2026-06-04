using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fruitables.Attributes;

// Logic kết hợp nhiều quyền khi kiểm tra authorization
public enum PermissionLogic
{
    // Chỉ cần 1 trong các quyền (OR) — vd: "product.create" HOẶC "product.edit"
    Or,
    
    // Phải có tất cả các quyền (AND) — vd: "product.create" VÀ "product.publish"
    And
}

// Attribute tùy chỉnh yêu cầu quyền cụ thể trước khi truy cập Controller/Action.
// Đăng ký toàn cục qua RequirePermissionFilter, attribute chỉ giữ cấu hình.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    // Danh sách quyền yêu cầu, định dạng "module.action" (vd: "product.create")
    public string[] Permissions { get; }
    
    // Logic kết hợp: mặc định Or — user chỉ cần 1 trong số các quyền
    public PermissionLogic Logic { get; set; } = PermissionLogic.Or;
    
    // Khởi tạo: truyền danh sách quyền, bắt buộc có ít nhất 1 phần tử
    public RequirePermissionAttribute(params string[] permissions)
    {
        // null check
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        
        // phải có ít nhất 1 quyền
        if (permissions.Length == 0)
        {
            throw new ArgumentException("Phải cung cấp ít nhất một quyền", nameof(permissions));
        }
    }
    
    // Được gọi trong filter pipeline.
    // Thực tế logic nằm ở RequirePermissionFilter — method này để trống vì attribute chỉ giữ dữ liệu.
    public void OnAuthorization(AuthorizationFilterContext context)
    {
    }
}
