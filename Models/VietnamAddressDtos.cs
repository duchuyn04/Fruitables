using System.Text.Json.Serialization;

namespace Fruitables.Models;

/// <summary>
/// DTO for Province/City from Vietnam Address API
/// </summary>
public class ProvinceDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("codename")]
    public string Codename { get; set; } = null!;
    
    [JsonPropertyName("division_type")]
    public string DivisionType { get; set; } = null!;
    
    [JsonPropertyName("phone_code")]
    public int PhoneCode { get; set; }
    
    [JsonPropertyName("districts")]
    public List<DistrictDto>? Districts { get; set; }
}

/// <summary>
/// DTO for District from Vietnam Address API
/// </summary>
public class DistrictDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("codename")]
    public string Codename { get; set; } = null!;
    
    [JsonPropertyName("division_type")]
    public string DivisionType { get; set; } = null!;
    
    [JsonPropertyName("province_code")]
    public int ProvinceCode { get; set; }
    
    [JsonPropertyName("wards")]
    public List<WardDto>? Wards { get; set; }
}

/// <summary>
/// DTO for Ward from Vietnam Address API
/// </summary>
public class WardDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("codename")]
    public string Codename { get; set; } = null!;
    
    [JsonPropertyName("division_type")]
    public string DivisionType { get; set; } = null!;
    
    [JsonPropertyName("district_code")]
    public int DistrictCode { get; set; }
}

/// <summary>
/// DTO for composing full address from components
/// </summary>
public class AddressComponentsDto
{
    public int ProvinceCode { get; set; }
    public string ProvinceName { get; set; } = null!;
    
    public int DistrictCode { get; set; }
    public string DistrictName { get; set; } = null!;
    
    public int WardCode { get; set; }
    public string WardName { get; set; } = null!;
    
    public string StreetAddress { get; set; } = null!;
}
