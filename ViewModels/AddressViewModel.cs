namespace Fruitables.ViewModels;

/// <summary>
/// ViewModel for displaying addresses in dropdowns and lists
/// </summary>
public class AddressViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    // Structured address fields
    public int ProvinceCode { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public int DistrictCode { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public int WardCode { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }

    /// <summary>
    /// Full address composed from structured fields
    /// </summary>
    public string FullAddress => $"{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}";

    /// <summary>
    /// Display text for dropdown options
    /// </summary>
    public string DisplayText => $"{FullName} - {Phone}";

    /// <summary>
    /// Shortened address for display (max 50 chars)
    /// </summary>
    public string ShortAddress => FullAddress.Length > 50
        ? FullAddress.Substring(0, 47) + "..."
        : FullAddress;
}
