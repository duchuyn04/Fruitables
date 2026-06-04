using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

// API endpoint tra cứu địa chỉ Việt Nam: tỉnh → quận/huyện → phường/xã.
// Route: /api/address
[ApiController]
[Route("api/address")]
public class AddressApiController : ControllerBase
{
    private readonly IVietnamAddressService _addressService;

    // Inject service tra cứu địa chỉ (dữ liệu từ API bên ngoài)
    public AddressApiController(IVietnamAddressService addressService)
    {
        _addressService = addressService;
    }

    // GET /api/address/provinces — trả về danh sách 63 tỉnh/thành, sắp xếp theo ABC
    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        try
        {
            var provinces = await _addressService.GetProvincesAsync();
            return Ok(provinces);
        }
        // API bên ngoài timeout > 5 giây
        catch (TimeoutException)
        {
            return StatusCode(504, new { error = "ApiTimeout", message = "API không phản hồi trong 5 giây" });
        }
        // API bên ngoài lỗi kết nối
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { error = "ServiceUnavailable", message = ex.Message });
        }
    }

    // GET /api/address/districts/{provinceCode} — tra cứu quận/huyện theo mã tỉnh
    [HttpGet("districts/{provinceCode:int}")]
    public async Task<IActionResult> GetDistricts(int provinceCode)
    {
        // Mã tỉnh phải > 0
        if (provinceCode <= 0)
        {
            return BadRequest(new { error = "InvalidProvinceCode", message = "Mã tỉnh không hợp lệ" });
        }

        try
        {
            var districts = await _addressService.GetDistrictsByProvinceAsync(provinceCode);
            return Ok(districts);
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { error = "ApiTimeout", message = "API không phản hồi trong 5 giây" });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { error = "ServiceUnavailable", message = ex.Message });
        }
    }

    // GET /api/address/wards/{districtCode} — tra cứu phường/xã theo mã quận/huyện
    [HttpGet("wards/{districtCode:int}")]
    public async Task<IActionResult> GetWards(int districtCode)
    {
        // Mã quận/huyện phải > 0
        if (districtCode <= 0)
        {
            return BadRequest(new { error = "InvalidDistrictCode", message = "Mã quận/huyện không hợp lệ" });
        }

        try
        {
            var wards = await _addressService.GetWardsByDistrictAsync(districtCode);
            return Ok(wards);
        }
        catch (TimeoutException)
        {
            return StatusCode(504, new { error = "ApiTimeout", message = "API không phản hồi trong 5 giây" });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { error = "ServiceUnavailable", message = ex.Message });
        }
    }
}
