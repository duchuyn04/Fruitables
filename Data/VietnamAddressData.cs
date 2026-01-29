using Fruitables.Models;

namespace Fruitables.Data;

/// <summary>
/// Static fallback data for Vietnam provinces when external API is unavailable
/// Contains all 63 provinces/cities of Vietnam
/// </summary>
public static class VietnamAddressData
{
    /// <summary>
    /// Get all 63 provinces/cities of Vietnam
    /// Data is sorted alphabetically by name
    /// </summary>
    public static List<ProvinceDto> GetProvinces()
    {
        return new List<ProvinceDto>
        {
            new() { Code = 89, Name = "An Giang", Codename = "an_giang", DivisionType = "tỉnh", PhoneCode = 296 },
            new() { Code = 77, Name = "Bà Rịa - Vũng Tàu", Codename = "ba_ria_vung_tau", DivisionType = "tỉnh", PhoneCode = 254 },
            new() { Code = 24, Name = "Bắc Giang", Codename = "bac_giang", DivisionType = "tỉnh", PhoneCode = 204 },
            new() { Code = 6, Name = "Bắc Kạn", Codename = "bac_kan", DivisionType = "tỉnh", PhoneCode = 209 },
            new() { Code = 95, Name = "Bạc Liêu", Codename = "bac_lieu", DivisionType = "tỉnh", PhoneCode = 291 },
            new() { Code = 27, Name = "Bắc Ninh", Codename = "bac_ninh", DivisionType = "tỉnh", PhoneCode = 222 },
            new() { Code = 83, Name = "Bến Tre", Codename = "ben_tre", DivisionType = "tỉnh", PhoneCode = 275 },
            new() { Code = 74, Name = "Bình Dương", Codename = "binh_duong", DivisionType = "tỉnh", PhoneCode = 274 },
            new() { Code = 52, Name = "Bình Định", Codename = "binh_dinh", DivisionType = "tỉnh", PhoneCode = 256 },
            new() { Code = 70, Name = "Bình Phước", Codename = "binh_phuoc", DivisionType = "tỉnh", PhoneCode = 271 },
            new() { Code = 60, Name = "Bình Thuận", Codename = "binh_thuan", DivisionType = "tỉnh", PhoneCode = 252 },
            new() { Code = 96, Name = "Cà Mau", Codename = "ca_mau", DivisionType = "tỉnh", PhoneCode = 290 },
            new() { Code = 92, Name = "Cần Thơ", Codename = "can_tho", DivisionType = "thành phố trung ương", PhoneCode = 292 },
            new() { Code = 4, Name = "Cao Bằng", Codename = "cao_bang", DivisionType = "tỉnh", PhoneCode = 206 },
            new() { Code = 48, Name = "Đà Nẵng", Codename = "da_nang", DivisionType = "thành phố trung ương", PhoneCode = 236 },
            new() { Code = 66, Name = "Đắk Lắk", Codename = "dak_lak", DivisionType = "tỉnh", PhoneCode = 262 },
            new() { Code = 67, Name = "Đắk Nông", Codename = "dak_nong", DivisionType = "tỉnh", PhoneCode = 261 },
            new() { Code = 11, Name = "Điện Biên", Codename = "dien_bien", DivisionType = "tỉnh", PhoneCode = 215 },
            new() { Code = 75, Name = "Đồng Nai", Codename = "dong_nai", DivisionType = "tỉnh", PhoneCode = 251 },
            new() { Code = 87, Name = "Đồng Tháp", Codename = "dong_thap", DivisionType = "tỉnh", PhoneCode = 277 },
            new() { Code = 64, Name = "Gia Lai", Codename = "gia_lai", DivisionType = "tỉnh", PhoneCode = 269 },
            new() { Code = 2, Name = "Hà Giang", Codename = "ha_giang", DivisionType = "tỉnh", PhoneCode = 219 },
            new() { Code = 35, Name = "Hà Nam", Codename = "ha_nam", DivisionType = "tỉnh", PhoneCode = 226 },
            new() { Code = 1, Name = "Hà Nội", Codename = "ha_noi", DivisionType = "thành phố trung ương", PhoneCode = 24 },
            new() { Code = 42, Name = "Hà Tĩnh", Codename = "ha_tinh", DivisionType = "tỉnh", PhoneCode = 239 },
            new() { Code = 30, Name = "Hải Dương", Codename = "hai_duong", DivisionType = "tỉnh", PhoneCode = 220 },
            new() { Code = 31, Name = "Hải Phòng", Codename = "hai_phong", DivisionType = "thành phố trung ương", PhoneCode = 225 },
            new() { Code = 93, Name = "Hậu Giang", Codename = "hau_giang", DivisionType = "tỉnh", PhoneCode = 293 },
            new() { Code = 79, Name = "Hồ Chí Minh", Codename = "ho_chi_minh", DivisionType = "thành phố trung ương", PhoneCode = 28 },
            new() { Code = 17, Name = "Hòa Bình", Codename = "hoa_binh", DivisionType = "tỉnh", PhoneCode = 218 },
            new() { Code = 33, Name = "Hưng Yên", Codename = "hung_yen", DivisionType = "tỉnh", PhoneCode = 221 },
            new() { Code = 56, Name = "Khánh Hòa", Codename = "khanh_hoa", DivisionType = "tỉnh", PhoneCode = 258 },
            new() { Code = 91, Name = "Kiên Giang", Codename = "kien_giang", DivisionType = "tỉnh", PhoneCode = 297 },
            new() { Code = 62, Name = "Kon Tum", Codename = "kon_tum", DivisionType = "tỉnh", PhoneCode = 260 },
            new() { Code = 12, Name = "Lai Châu", Codename = "lai_chau", DivisionType = "tỉnh", PhoneCode = 213 },
            new() { Code = 68, Name = "Lâm Đồng", Codename = "lam_dong", DivisionType = "tỉnh", PhoneCode = 263 },
            new() { Code = 20, Name = "Lạng Sơn", Codename = "lang_son", DivisionType = "tỉnh", PhoneCode = 205 },
            new() { Code = 10, Name = "Lào Cai", Codename = "lao_cai", DivisionType = "tỉnh", PhoneCode = 214 },
            new() { Code = 80, Name = "Long An", Codename = "long_an", DivisionType = "tỉnh", PhoneCode = 272 },
            new() { Code = 36, Name = "Nam Định", Codename = "nam_dinh", DivisionType = "tỉnh", PhoneCode = 228 },
            new() { Code = 40, Name = "Nghệ An", Codename = "nghe_an", DivisionType = "tỉnh", PhoneCode = 238 },
            new() { Code = 37, Name = "Ninh Bình", Codename = "ninh_binh", DivisionType = "tỉnh", PhoneCode = 229 },
            new() { Code = 58, Name = "Ninh Thuận", Codename = "ninh_thuan", DivisionType = "tỉnh", PhoneCode = 259 },
            new() { Code = 25, Name = "Phú Thọ", Codename = "phu_tho", DivisionType = "tỉnh", PhoneCode = 210 },
            new() { Code = 54, Name = "Phú Yên", Codename = "phu_yen", DivisionType = "tỉnh", PhoneCode = 257 },
            new() { Code = 44, Name = "Quảng Bình", Codename = "quang_binh", DivisionType = "tỉnh", PhoneCode = 232 },
            new() { Code = 49, Name = "Quảng Nam", Codename = "quang_nam", DivisionType = "tỉnh", PhoneCode = 235 },
            new() { Code = 51, Name = "Quảng Ngãi", Codename = "quang_ngai", DivisionType = "tỉnh", PhoneCode = 255 },
            new() { Code = 22, Name = "Quảng Ninh", Codename = "quang_ninh", DivisionType = "tỉnh", PhoneCode = 203 },
            new() { Code = 45, Name = "Quảng Trị", Codename = "quang_tri", DivisionType = "tỉnh", PhoneCode = 233 },
            new() { Code = 94, Name = "Sóc Trăng", Codename = "soc_trang", DivisionType = "tỉnh", PhoneCode = 299 },
            new() { Code = 14, Name = "Sơn La", Codename = "son_la", DivisionType = "tỉnh", PhoneCode = 212 },
            new() { Code = 72, Name = "Tây Ninh", Codename = "tay_ninh", DivisionType = "tỉnh", PhoneCode = 276 },
            new() { Code = 34, Name = "Thái Bình", Codename = "thai_binh", DivisionType = "tỉnh", PhoneCode = 227 },
            new() { Code = 19, Name = "Thái Nguyên", Codename = "thai_nguyen", DivisionType = "tỉnh", PhoneCode = 208 },
            new() { Code = 38, Name = "Thanh Hóa", Codename = "thanh_hoa", DivisionType = "tỉnh", PhoneCode = 237 },
            new() { Code = 46, Name = "Thừa Thiên Huế", Codename = "thua_thien_hue", DivisionType = "tỉnh", PhoneCode = 234 },
            new() { Code = 82, Name = "Tiền Giang", Codename = "tien_giang", DivisionType = "tỉnh", PhoneCode = 273 },
            new() { Code = 84, Name = "Trà Vinh", Codename = "tra_vinh", DivisionType = "tỉnh", PhoneCode = 294 },
            new() { Code = 8, Name = "Tuyên Quang", Codename = "tuyen_quang", DivisionType = "tỉnh", PhoneCode = 207 },
            new() { Code = 86, Name = "Vĩnh Long", Codename = "vinh_long", DivisionType = "tỉnh", PhoneCode = 270 },
            new() { Code = 26, Name = "Vĩnh Phúc", Codename = "vinh_phuc", DivisionType = "tỉnh", PhoneCode = 211 },
            new() { Code = 15, Name = "Yên Bái", Codename = "yen_bai", DivisionType = "tỉnh", PhoneCode = 216 }
        }.OrderBy(p => p.Name).ToList();
    }
}
