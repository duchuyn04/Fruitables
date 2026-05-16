using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Services.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Fruitables.Services;

/// <summary>
/// Service xử lý địa chỉ Việt Nam
/// - Gọi API ngoài: provinces.open-api.vn (timeout 10s)
/// - Fallback: dữ liệu tĩnh trong VietnamAddressData.cs khi API lỗi
/// - Kết quả luôn được sắp xếp theo tên (A-Z) tiếng Việt
/// </summary>
public class VietnamAddressService : IVietnamAddressService
{
    private readonly HttpClient _httpClient;

    // Cấu hình JSON deserializer: không phân biệt hoa thường tên property
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Constructor: nhận HttpClient đã được cấu hình BaseAddress trong Program.cs
    public VietnamAddressService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ============================================================
    // LẤY DANH SÁCH TỈNH/THÀNH PHỐ
    // ============================================================

    /// <summary>
    /// Lấy 63 tỉnh/thành phố
    /// - Ưu tiên: gọi API ngoài
    /// - Fallback: trả dữ liệu tĩnh nếu API lỗi/timeout
    /// - Kết quả: sắp xếp A-Z theo tiếng Việt
    /// </summary>
    public async Task<List<ProvinceDto>> GetProvincesAsync()
    {
        try
        {
            // Gọi endpoint: GET {BaseAddress}/p/
            var response = await _httpClient.GetAsync("p/");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var provinces = JsonSerializer.Deserialize<List<ProvinceDto>>(json, JsonOptions);

            // Nếu API trả về rỗng → dùng fallback
            if (provinces == null || provinces.Count == 0)
                return VietnamAddressData.GetProvinces();

            // Sắp xếp theo tên tiếng Việt (có dấu)
            return provinces
                .OrderBy(p => p.Name, StringComparer.Create(new CultureInfo("vi-VN"), false))
                .ToList();
        }
        catch (TaskCanceledException)
        {
            // Timeout → fallback
            return VietnamAddressData.GetProvinces();
        }
        catch (HttpRequestException)
        {
            // API không reachable → fallback
            return VietnamAddressData.GetProvinces();
        }
        catch (Exception)
        {
            // Lỗi khác → fallback
            return VietnamAddressData.GetProvinces();
        }
    }

    // ============================================================
    // LẤY DANH SÁCH QUẬN/HUYỆN THEO TỈNH
    // ============================================================

    /// <summary>
    /// Lấy quận/huyện theo mã tỉnh
    /// - Gọi API: GET {BaseAddress}/p/{provinceCode}?depth=2
    /// - Gán ProvinceCode cho từng district (đảm bảo dữ liệu nhất quán)
    /// - Kết quả: sắp xếp A-Z theo tiếng Việt
    /// - Ném lỗi nếu API thất bại (KHÔNG có fallback cho district)
    /// </summary>
    public async Task<List<DistrictDto>> GetDistrictsByProvinceAsync(int provinceCode)
    {
        try
        {
            // depth=2: API trả về province kèm danh sách districts bên trong
            var response = await _httpClient.GetAsync($"p/{provinceCode}?depth=2");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var province = JsonSerializer.Deserialize<ProvinceDto>(json, JsonOptions);

            if (province?.Districts == null)
                return new List<DistrictDto>();

            // Tạo bản sao district, gán ProvinceCode và sắp xếp
            return province.Districts
                .Select(d => new DistrictDto
                {
                    Code = d.Code,
                    Name = d.Name,
                    Codename = d.Codename,
                    DivisionType = d.DivisionType,
                    ProvinceCode = provinceCode,  // Gán mã tỉnh cho nhất quán
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

    // ============================================================
    // LẤY DANH SÁCH PHƯỜNG/XÃ THEO QUẬN
    // ============================================================

    /// <summary>
    /// Lấy phường/xã theo mã quận/huyện
    /// - Gọi API: GET {BaseAddress}/d/{districtCode}?depth=2
    /// - Gán DistrictCode cho từng ward (đảm bảo dữ liệu nhất quán)
    /// - Kết quả: sắp xếp A-Z theo tiếng Việt
    /// - Ném lỗi nếu API thất bại (KHÔNG có fallback cho ward)
    /// </summary>
    public async Task<List<WardDto>> GetWardsByDistrictAsync(int districtCode)
    {
        try
        {
            // depth=2: API trả về district kèm danh sách wards bên trong
            var response = await _httpClient.GetAsync($"d/{districtCode}?depth=2");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var district = JsonSerializer.Deserialize<DistrictDto>(json, JsonOptions);

            if (district?.Wards == null)
                return new List<WardDto>();

            // Tạo bản sao ward, gán DistrictCode và sắp xếp
            return district.Wards
                .Select(w => new WardDto
                {
                    Code = w.Code,
                    Name = w.Name,
                    Codename = w.Codename,
                    DivisionType = w.DivisionType,
                    DistrictCode = districtCode  // Gán mã quận cho nhất quán
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

    // ============================================================
    // GHÉP ĐỊA CHỈ ĐẦY ĐỦ
    // ============================================================

    /// <summary>
    /// Ghép địa chỉ đầy đủ từ các thành phần
    /// Format: "{Số nhà + tên đường}, {Phường/Xã}, {Quận/Huyện}, {Tỉnh/TP}"
    /// VD: "123 Nguyễn Huệ, Bến Nghé, Quận 1, Hồ Chí Minh"
    /// </summary>
    public string ComposeFullAddress(AddressComponentsDto components)
    {
        if (components == null)
            throw new ArgumentNullException(nameof(components));

        return $"{components.StreetAddress}, {components.WardName}, {components.DistrictName}, {components.ProvinceName}";
    }

    // ============================================================
    // BỎ DẤU TIẾNG VIỆT
    // ============================================================

    /// <summary>
    /// Loại bỏ dấu tiếng Việt
    /// VD: "Hà Nội" → "Ha Noi", "Đà Nẵng" → "Da Nang"
    /// Dùng để so sánh/tìm kiếm không phân biệt dấu
    /// </summary>
    public string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Bước 1: Normalize FormD - tách ký tự gốc và dấu thành ký tự riêng
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        // Bước 2: Loại bỏ các ký tự "non-spacing mark" (dấu thanh, dấu mũ...)
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Bước 3: Normalize lại FormC - ghép các ký tự rời rạc
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Bước 4: Xử lý đ/Đ - ký tự này không decompose được nên thay thủ công
        result = result.Replace('đ', 'd').Replace('Đ', 'D');

        return result;
    }

    // ============================================================
    // CHỐNG XSS CHO ĐỊA CHỈ ĐƯỜNG
    // ============================================================

    /// <summary>
    /// Làm sạch địa chỉ đường - loại bỏ mã độc XSS
    /// - Xóa HTML tags: <script>, <img>, ...
    /// - Xóa javascript: protocol
    /// - Xóa event handlers: onclick, onerror, onload, ...
    /// </summary>
    public string SanitizeStreetAddress(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        // Xóa tất cả HTML tags
        var result = Regex.Replace(input, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);

        // Xóa javascript: protocol (VD: javascript:alert(1))
        result = Regex.Replace(result, @"javascript\s*:", string.Empty, RegexOptions.IgnoreCase);

        // Xóa event handlers dạng: onclick="..." hoặc onerror=alert(1)
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*['""][^'""]*['""]", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*\S+", string.Empty, RegexOptions.IgnoreCase);

        return result.Trim();
    }

    // ============================================================
    // TÌM KIẾM KHÔNG PHÂN BIỆT DẤU
    // ============================================================

    /// <summary>
    /// Lọc danh sách theo từ khóa - không phân biệt dấu và hoa thường
    /// VD: tìm "ha noi" khớp với "Hà Nội"
    /// Trả về tất cả nếu keyword rỗng
    /// </summary>
    public IEnumerable<T> FilterByKeyword<T>(IEnumerable<T> items, string? keyword, Func<T, string> nameSelector)
    {
        if (items == null)
            return Enumerable.Empty<T>();

        // Keyword rỗng → trả về toàn bộ
        if (string.IsNullOrWhiteSpace(keyword))
            return items;

        // Chuẩn hóa keyword: bỏ dấu + lowercase
        var normalizedKeyword = RemoveDiacritics(keyword).ToLowerInvariant();

        // Lọc: chuẩn hóa tên từng item rồi so sánh contains
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
