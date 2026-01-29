using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

/// <summary>
/// Service interface for Vietnam Address API operations
/// </summary>
public interface IVietnamAddressService
{
    /// <summary>
    /// Get all provinces/cities in Vietnam
    /// </summary>
    Task<List<ProvinceDto>> GetProvincesAsync();
    
    /// <summary>
    /// Get districts by province code
    /// </summary>
    Task<List<DistrictDto>> GetDistrictsByProvinceAsync(int provinceCode);
    
    /// <summary>
    /// Get wards by district code
    /// </summary>
    Task<List<WardDto>> GetWardsByDistrictAsync(int districtCode);
    
    /// <summary>
    /// Compose full address from components
    /// </summary>
    string ComposeFullAddress(AddressComponentsDto components);
    
    /// <summary>
    /// Remove Vietnamese diacritics from text
    /// </summary>
    string RemoveDiacritics(string text);
    
    /// <summary>
    /// Sanitize street address to remove XSS content
    /// Removes HTML tags, script tags, event handlers, and javascript: protocol
    /// </summary>
    string SanitizeStreetAddress(string? input);
    
    /// <summary>
    /// Filter a list of names by keyword using case-insensitive, diacritics-insensitive matching
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="items">List of items to filter</param>
    /// <param name="keyword">Search keyword</param>
    /// <param name="nameSelector">Function to extract the name from each item</param>
    /// <returns>Filtered list of items matching the keyword</returns>
    IEnumerable<T> FilterByKeyword<T>(IEnumerable<T> items, string? keyword, Func<T, string> nameSelector);
}
