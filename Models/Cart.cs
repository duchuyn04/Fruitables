using System.ComponentModel.DataAnnotations;

namespace Fruitables.Models;

public class Cart
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    [MaxLength(255)]
    public string? SessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
