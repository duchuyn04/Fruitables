using Fruitables.Models;

namespace Fruitables.Data;

public static class VietnamAddressData
{
    public static List<ProvinceDto> GetProvinces()
    {
        return new List<ProvinceDto>
        {
            new() { Id = "92", Name = "Thành phố Cần Thơ" },
            new() { Id = "46", Name = "Thành phố Huế" },
            new() { Id = "01", Name = "Thành phố Hà Nội" },
            new() { Id = "31", Name = "Thành phố Hải Phòng" },
            new() { Id = "79", Name = "Thành phố Hồ Chí Minh" },
            new() { Id = "48", Name = "Thành phố Đà Nẵng" },
            new() { Id = "91", Name = "Tỉnh An Giang" },
            new() { Id = "24", Name = "Tỉnh Bắc Ninh" },
            new() { Id = "04", Name = "Tỉnh Cao Bằng" },
            new() { Id = "96", Name = "Tỉnh Cà Mau" },
            new() { Id = "52", Name = "Tỉnh Gia Lai" },
            new() { Id = "42", Name = "Tỉnh Hà Tĩnh" },
            new() { Id = "33", Name = "Tỉnh Hưng Yên" },
            new() { Id = "56", Name = "Tỉnh Khánh Hòa" },
            new() { Id = "12", Name = "Tỉnh Lai Châu" },
            new() { Id = "15", Name = "Tỉnh Lào Cai" },
            new() { Id = "68", Name = "Tỉnh Lâm Đồng" },
            new() { Id = "20", Name = "Tỉnh Lạng Sơn" },
            new() { Id = "40", Name = "Tỉnh Nghệ An" },
            new() { Id = "37", Name = "Tỉnh Ninh Bình" },
            new() { Id = "25", Name = "Tỉnh Phú Thọ" },
            new() { Id = "51", Name = "Tỉnh Quảng Ngãi" },
            new() { Id = "22", Name = "Tỉnh Quảng Ninh" },
            new() { Id = "44", Name = "Tỉnh Quảng Trị" },
            new() { Id = "14", Name = "Tỉnh Sơn La" },
            new() { Id = "38", Name = "Tỉnh Thanh Hóa" },
            new() { Id = "19", Name = "Tỉnh Thái Nguyên" },
            new() { Id = "08", Name = "Tỉnh Tuyên Quang" },
            new() { Id = "80", Name = "Tỉnh Tây Ninh" },
            new() { Id = "86", Name = "Tỉnh Vĩnh Long" },
            new() { Id = "11", Name = "Tỉnh Điện Biên" },
            new() { Id = "66", Name = "Tỉnh Đắk Lắk" },
            new() { Id = "75", Name = "Tỉnh Đồng Nai" },
            new() { Id = "82", Name = "Tỉnh Đồng Tháp" }
        }.OrderBy(p => p.Name).ToList();
    }
}
