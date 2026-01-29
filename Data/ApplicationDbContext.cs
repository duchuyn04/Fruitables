using Microsoft.EntityFrameworkCore;
using Fruitables.Models;

namespace Fruitables.Data;

public class ApplicationDbContext : DbContext
{
    // Pre-generated BCrypt hash for "Admin@123" password
    // Generated using BCrypt.Net.BCrypt.HashPassword("Admin@123")
    private const string AdminPasswordHash = "$2a$11$lA/jMR6h6Qga83lrdc0xd.Fx1TLBOiefaI1vAvCcVTjhYFqTYisHO";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductLog> ProductLogs => Set<ProductLog>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderStatusAuditLog> OrderStatusAuditLogs => Set<OrderStatusAuditLog>();
    public DbSet<AuditLogAttachment> AuditLogAttachments => Set<AuditLogAttachment>();
    public DbSet<UserAccountLog> UserAccountLogs => Set<UserAccountLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category - Self-referencing
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(c => c.Parent)
                  .WithMany(c => c.Children)
                  .HasForeignKey(c => c.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Product - Tag (Many-to-Many)
        modelBuilder.Entity<Product>()
            .HasMany(p => p.Tags)
            .WithMany(t => t.Products)
            .UsingEntity(j => j.ToTable("ProductTagMapping"));

        // Cart - User (One-to-One)
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasOne(c => c.User)
                  .WithOne(u => u.Cart)
                  .HasForeignKey<Cart>(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // Index cho lịch sử đơn hàng
            entity.HasIndex(e => e.Status); // Index cho lọc theo trạng thái
            entity.HasIndex(e => e.AddressId);
            entity.HasOne(o => o.Address)
                  .WithMany(a => a.Orders)
                  .HasForeignKey(o => o.AddressId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Wishlist - Unique constraint
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        });

        // Coupon
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Setting
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasOne(v => v.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(v => v.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductLog
        modelBuilder.Entity<ProductLog>(entity =>
        {
            entity.HasOne(l => l.Product)
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(l => l.Admin)
                  .WithMany()
                  .HasForeignKey(l => l.AdminId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CreatedAt);
        });

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Order)
                  .WithMany(o => o.StatusHistory)
                  .HasForeignKey(h => h.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(h => h.Admin)
                  .WithMany()
                  .HasForeignKey(h => h.AdminId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // OrderStatusAuditLog
        modelBuilder.Entity<OrderStatusAuditLog>(entity =>
        {
            entity.HasOne(a => a.Order)
                  .WithMany()
                  .HasForeignKey(a => a.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.AdminId);
        });

        // AuditLogAttachment
        modelBuilder.Entity<AuditLogAttachment>(entity =>
        {
            entity.HasOne(a => a.AuditLog)
                  .WithMany(l => l.Attachments)
                  .HasForeignKey(a => a.AuditLogId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.AuditLogId);
        });

        // UserAccountLog
        modelBuilder.Entity<UserAccountLog>(entity =>
        {
            entity.HasOne(l => l.User)
                  .WithMany(u => u.AccountLogs)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.Admin)
                  .WithMany()
                  .HasForeignKey(l => l.AdminId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AdminId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // User - LockedByAdmin relationship
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(u => u.LockedByAdmin)
                  .WithMany()
                  .HasForeignKey(u => u.LockedByAdminId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Seed Admin User
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "Admin User",
                Email = "admin@fruitables.com",
                Password = AdminPasswordHash,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Name = "Super Admin",
                Email = "superadmin@fruitables.com",
                Password = AdminPasswordHash,
                Role = UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

    }

    /// <summary>
    /// Seeds default settings if they don't exist (from original HTML template)
    /// </summary>
    public async Task SeedDefaultSettingsAsync()
    {
        var defaultSettings = new Dictionary<string, (string Value, string Group)>
        {
            // General Settings
            ["site_name"] = ("Fruitables", "General"),
            
            // SEO Settings
            ["meta_title"] = ("Fruitables - Vegetable Website Template", "SEO"),
            ["meta_description"] = ("Fresh organic vegetables and fruits delivered to your door", "SEO"),
            ["meta_keywords"] = ("organic, vegetables, fruits, fresh, healthy, food", "SEO"),
            
            // Contact Settings
            ["contact_address"] = ("1429 Netus Rd, NY 48247", "Contact"),
            ["contact_phone"] = ("+0123 4567 8910", "Contact"),
            ["contact_email"] = ("info@fruitables.com", "Contact"),
            ["contact_working_hours"] = ("Mon - Sat: 8:00 - 18:00", "Contact"),
            ["contact_map_embed"] = (@"<iframe class=""rounded w-100"" style=""height: 400px;"" src=""https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d387191.33750346623!2d-73.97968099999999!3d40.6974881!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x89c24fa5d33f083b%3A0xc80b8f06e177fe62!2sNew%20York%2C%20NY%2C%20USA!5e0!3m2!1sen!2sbd!4v1694259649153!5m2!1sen!2sbd"" loading=""lazy"" referrerpolicy=""no-referrer-when-downgrade""></iframe>", "Contact"),
            
            // Social Settings
            ["social_facebook"] = ("https://facebook.com/fruitables", "Social"),
            ["social_twitter"] = ("https://twitter.com/fruitables", "Social"),
            ["social_instagram"] = ("https://instagram.com/fruitables", "Social"),
            ["social_youtube"] = ("https://youtube.com/fruitables", "Social"),
            ["social_linkedin"] = ("https://linkedin.com/company/fruitables", "Social")
        };

        // Get existing keys
        var existingKeys = await Settings.Select(s => s.Key).ToListAsync();
        
        // Add missing settings
        var settingsToAdd = defaultSettings
            .Where(kv => !existingKeys.Contains(kv.Key))
            .Select(kv => new Setting { Key = kv.Key, Value = kv.Value.Value, Group = kv.Value.Group })
            .ToList();

        if (settingsToAdd.Count > 0)
        {
            Settings.AddRange(settingsToAdd);
            await SaveChangesAsync();
        }
    }
}
