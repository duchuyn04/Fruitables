using System.Text.Json;
using Fruitables.Models;

namespace Fruitables.Helpers;

public static class AddressSnapshotHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    
    public static string ToSnapshot(Address address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        var snapshot = new
        {
            fullName = address.FullName,
            phone = address.Phone,
            provinceCode = address.ProvinceCode,
            provinceName = address.ProvinceName,
            districtCode = address.DistrictCode,
            districtName = address.DistrictName,
            wardCode = address.WardCode,
            wardName = address.WardName,
            streetAddress = address.StreetAddress,
            fullAddress = address.FullAddress
        };

        return JsonSerializer.Serialize(snapshot, Options);
    }

    
    public static Address? FromSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, Options);
            if (data == null)
                return null;

            return new Address
            {
                FullName = data.TryGetValue("fullName", out var fn) ? fn.GetString() ?? string.Empty : string.Empty,
                Phone = data.TryGetValue("phone", out var ph) ? ph.GetString() ?? string.Empty : string.Empty,
                ProvinceCode = data.TryGetValue("provinceCode", out var pc) ? pc.GetInt32() : 0,
                ProvinceName = data.TryGetValue("provinceName", out var pn) ? pn.GetString() ?? string.Empty : string.Empty,
                DistrictCode = data.TryGetValue("districtCode", out var dc) ? dc.GetInt32() : 0,
                DistrictName = data.TryGetValue("districtName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty,
                WardCode = data.TryGetValue("wardCode", out var wc) ? wc.GetInt32() : 0,
                WardName = data.TryGetValue("wardName", out var wn) ? wn.GetString() ?? string.Empty : string.Empty,
                StreetAddress = data.TryGetValue("streetAddress", out var sa) ? sa.GetString() ?? string.Empty : string.Empty
            };
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            return null;
        }
    }
}
