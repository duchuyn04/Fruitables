using Fruitables.Models;

namespace Fruitables.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Existing repositories
    ICategoryRepository Categories { get; }
    IRepository<Product> Products { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Setting> Settings { get; }
    IRepository<Review> Reviews { get; }
    IReviewRepository ReviewRepository { get; }
    IReviewReportRepository ReviewReports { get; }
    IRepository<ContactMessage> Contacts { get; }
    IRepository<Testimonial> Testimonials { get; }
    IRepository<User> Users { get; }
    
    // New repositories for CartService
    IRepository<Cart> Carts { get; }
    IRepository<CartItem> CartItems { get; }
    
    // New repository for ProductService
    IRepository<ProductImage> ProductImages { get; }
    
    // New repositories for ProductAdminService
    IRepository<ProductVariant> ProductVariants { get; }
    IRepository<ProductTag> ProductTags { get; }
    IRepository<ProductLog> ProductLogs { get; }
    
    // New repository for Address
    IRepository<Address> Addresses { get; }
    
    // RBAC repositories
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<UserRoleMapping> UserRoleMappings { get; }
    IRepository<RolePermission> RolePermissions { get; }
    IRepository<RbacAuditLog> RbacAuditLogs { get; }
    
    Task<int> SaveChangesAsync();
}
