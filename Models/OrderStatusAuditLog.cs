using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fruitables.Models;

/// <summary>
/// Audit log entry for order status changes.
/// Records who made the change, when, and what changed.
/// </summary>
public class OrderStatusAuditLog
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    // Admin user info
    [Required]
    public int AdminId { get; set; }

    [Required]
    [MaxLength(100)]
    public string AdminName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AdminEmail { get; set; } = string.Empty;

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Status snapshot - old values
    public OrderStatus OldOrderStatus { get; set; }
    public PaymentStatus OldPaymentStatus { get; set; }

    // Status snapshot - new values
    public OrderStatus NewOrderStatus { get; set; }
    public PaymentStatus NewPaymentStatus { get; set; }

    // Notes
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual ICollection<AuditLogAttachment> Attachments { get; set; } = new List<AuditLogAttachment>();
}
