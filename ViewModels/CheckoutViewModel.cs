using System.ComponentModel.DataAnnotations;
using Fruitables.Models;

namespace Fruitables.ViewModels;

public class CheckoutViewModel
{
    // For selecting existing address
    public int? SelectedAddressId { get; set; }

    // Full name field (combines FirstName + LastName)
    public string? FullName { get; set; }

    [Required, Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    // Structured address fields
    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
    [Display(Name = "Tỉnh/Thành phố")]
    public int ProvinceCode { get; set; }

    public string? ProvinceName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
    [Display(Name = "Quận/Huyện")]
    public int DistrictCode { get; set; }

    public string? DistrictName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
    [Display(Name = "Phường/Xã")]
    public int WardCode { get; set; }

    public string? WardName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường")]
    [MaxLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
    [Display(Name = "Số nhà, Tên đường")]
    public string StreetAddress { get; set; } = string.Empty;

    [Display(Name = "Town/City")]
    public string? City { get; set; }

    public string? Country { get; set; }

    [Display(Name = "Postcode/Zip")]
    public string? Postcode { get; set; }

    [Required, Phone]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress, Display(Name = "Email Address")]
    public string? Email { get; set; }

    public bool CreateAccount { get; set; }
    public bool ShipToDifferentAddress { get; set; }

    [Display(Name = "Order Notes")]
    public string? Notes { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public ShippingMethod ShippingMethod { get; set; }

    // Save address for future use
    public bool SaveAddress { get; set; } = true;

    // Set as default address
    public bool SetAsDefault { get; set; } = false;

    // Cart summary
    public CartViewModel Cart { get; set; } = new();

    /// <summary>
    /// Composes full address from structured fields
    /// Format: "{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}"
    /// </summary>
    public string ComposedFullAddress =>
        $"{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}";
}
