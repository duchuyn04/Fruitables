using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fruitables.Models;

public class Address
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại phải có 10-11 chữ số")]
    public string Phone { get; set; } = string.Empty;

    // Structured address fields
    [Required]
    public int ProvinceCode { get; set; }

    [Required, MaxLength(100)]
    public string ProvinceName { get; set; } = string.Empty;

    [Required]
    public int DistrictCode { get; set; }

    [Required, MaxLength(100)]
    public string DistrictName { get; set; } = string.Empty;

    [Required]
    public int WardCode { get; set; }

    [Required, MaxLength(100)]
    public string WardName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string StreetAddress { get; set; } = string.Empty;

    /// <summary>
    /// Computed full address from structured fields
    /// Format: "{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}"
    /// </summary>
    [NotMapped]
    public string FullAddress => $"{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}";

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
