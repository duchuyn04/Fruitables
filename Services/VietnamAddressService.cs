using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Services.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Fruitables.Services;

/// <summary>
/// Service for Vietnam Address API operations
/// Calls provinces.open-api.vn API with 10 second timeout
/// Falls back to static data when API is unavailable
/// Results are sorted alphabetically by Name
/// </summary>
public class VietnamAddressService : IVietnamAddressService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VietnamAddressService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get all provinces/cities in Vietnam
    /// Returns 63 provinces sorted alphabetically by Name
    /// Falls back to static data when API is unavailable
    /// </summary>
    public async Task<List<ProvinceDto>> GetProvincesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("p/");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceDto>>(json, JsonOptions);
            
            if (provinces == null || provinces.Count == 0)
                return VietnamAddressData.GetProvinces();
            
            // Sort alphabetically by Name using Vietnamese culture
            return provinces
                .OrderBy(p => p.Name, StringComparer.Create(new CultureInfo("vi-VN"), false))
                .ToList();
        }
        catch (TaskCanceledException)
        {
            // API timeout - use fallback data
            return VietnamAddressData.GetProvinces();
        }
        catch (HttpRequestException)
        {
            // API unavailable - use fallback data
            return VietnamAddressData.GetProvinces();
        }
        catch (Exception)
        {
            // Any other error - use fallback data
            return VietnamAddressData.GetProvinces();
        }
    }

    /// <summary>
    /// Get districts by province code
    /// All returned districts have ProvinceCode matching the input
    /// </summary>
    public async Task<List<DistrictDto>> GetDistrictsByProvinceAsync(int provinceCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"p/{provinceCode}?depth=2");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var province = JsonSerializer.Deserialize<ProvinceDto>(json, JsonOptions);
            
            if (province?.Districts == null)
                return new List<DistrictDto>();
            
            // Ensure all districts have correct ProvinceCode and sort alphabetically
            return province.Districts
                .Select(d => new DistrictDto
                {
                    Code = d.Code,
                    Name = d.Name,
                    Codename = d.Codename,
                    DivisionType = d.DivisionType,
                    ProvinceCode = provinceCode,
                    Wards = d.Wards
                })
                .OrderBy(d => d.Name, StringComparer.Create(new CultureInfo("vi-VN"), false))
                .ToList();
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException("API request timed out after 5 seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to fetch districts for province {provinceCode}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get wards by district code
    /// All returned wards have DistrictCode matching the input
    /// </summary>
    public async Task<List<WardDto>> GetWardsByDistrictAsync(int districtCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"d/{districtCode}?depth=2");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var district = JsonSerializer.Deserialize<DistrictDto>(json, JsonOptions);
            
            if (district?.Wards == null)
                return new List<WardDto>();
            
            // Ensure all wards have correct DistrictCode and sort alphabetically
            return district.Wards
                .Select(w => new WardDto
                {
                    Code = w.Code,
                    Name = w.Name,
                    Codename = w.Codename,
                    DivisionType = w.DivisionType,
                    DistrictCode = districtCode
                })
                .OrderBy(w => w.Name, StringComparer.Create(new CultureInfo("vi-VN"), false))
                .ToList();
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException("API request timed out after 5 seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to fetch wards for district {districtCode}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Compose full address from components
    /// Format: "{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}"
    /// </summary>
    public string ComposeFullAddress(AddressComponentsDto components)
    {
        if (components == null)
            throw new ArgumentNullException(nameof(components));
            
        return $"{components.StreetAddress}, {components.WardName}, {components.DistrictName}, {components.ProvinceName}";
    }

    /// <summary>
    /// Remove Vietnamese diacritics from text
    /// Converts "Hà Nội" to "Ha Noi"
    /// </summary>
    public string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // Normalize to FormD (decomposed form)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            // Skip non-spacing marks (diacritics)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Handle special Vietnamese characters that don't decompose properly
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        
        // Handle đ/Đ separately as they don't decompose
        result = result.Replace('đ', 'd').Replace('Đ', 'D');
        
        return result;
    }

    /// <summary>
    /// Sanitize street address to remove XSS content
    /// Removes HTML tags, script tags, event handlers, and javascript: protocol
    /// </summary>
    public string SanitizeStreetAddress(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        // Remove HTML tags
        var result = Regex.Replace(input, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        
        // Remove javascript: protocol
        result = Regex.Replace(result, @"javascript\s*:", string.Empty, RegexOptions.IgnoreCase);
        
        // Remove event handlers (onclick, onerror, onload, etc.)
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*['""][^'""]*['""]", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*\S+", string.Empty, RegexOptions.IgnoreCase);

        return result.Trim();
    }

    /// <summary>
    /// Filter a list of items by keyword using case-insensitive, diacritics-insensitive matching
    /// Returns all items if keyword is null, empty, or whitespace
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="items">List of items to filter</param>
    /// <param name="keyword">Search keyword</param>
    /// <param name="nameSelector">Function to extract the name from each item</param>
    /// <returns>Filtered list of items matching the keyword</returns>
    public IEnumerable<T> FilterByKeyword<T>(IEnumerable<T> items, string? keyword, Func<T, string> nameSelector)
    {
        if (items == null)
            return Enumerable.Empty<T>();
            
        // Return all items if keyword is empty or whitespace
        if (string.IsNullOrWhiteSpace(keyword))
            return items;

        // Normalize keyword: remove diacritics and convert to lowercase
        var normalizedKeyword = RemoveDiacritics(keyword).ToLowerInvariant();
        
        // Filter items where the normalized name contains the normalized keyword
        return items.Where(item =>
        {
            var name = nameSelector(item);
            if (string.IsNullOrEmpty(name))
                return false;
                
            var normalizedName = RemoveDiacritics(name).ToLowerInvariant();
            return normalizedName.Contains(normalizedKeyword);
        });
    }
}
