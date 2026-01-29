using System.Text.Json;
using Fruitables.Models;

namespace Fruitables.Helpers;

/// <summary>
/// Helper class for serializing and deserializing Address snapshots to/from JSON
/// </summary>
public static class AddressSnapshotHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Converts an Address object to a JSON snapshot string
    /// </summary>
    /// <param name="address">The address to serialize</param>
    /// <returns>JSON string representation of the address</returns>
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

    /// <summary>
    /// Converts a JSON snapshot string back to an Address object
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Address object or null if parsing fails</returns>
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
            // Log error if needed, but return null gracefully
            // InvalidOperationException is thrown when JSON element type doesn't match expected type
            return null;
        }
    }
}
