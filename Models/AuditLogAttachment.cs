using System.ComponentModel.DataAnnotations;

namespace Fruitables.Models;

/// <summary>
/// Attachment file linked to an audit log entry.
/// Used for storing evidence when cancelling or returning orders.
/// </summary>
public class AuditLogAttachment
{
    public int Id { get; set; }

    [Required]
    public int AuditLogId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual OrderStatusAuditLog AuditLog { get; set; } = null!;
}
