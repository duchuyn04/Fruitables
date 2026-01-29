using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;

namespace Fruitables.Controllers;

/// <summary>
/// API Controller for Vietnam Address operations
/// Provides endpoints for provinces, districts, and wards
/// Route: /api/address
/// </summary>
[ApiController]
[Route("api/address")]
public class AddressApiController : ControllerBase
{
    private readonly IVietnamAddressService _addressService;

    public AddressApiController(IVietnamAddressService addressService)
    {
        _addressService = addressService;
    }

    /// <summary>
    /// Get all provinces/cities in Vietnam
    /// GET /api/address/provinces
    /// Returns 63 provinces sorted alphabetically
    /// </summary>
    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        try
        {
            var provinces = await _addressService.GetProvincesAsync();
            return Ok(provinces);
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

    /// <summary>
    /// Get districts by province code
    /// GET /api/address/districts/{provinceCode}
    /// Returns districts for the specified province
    /// </summary>
    [HttpGet("districts/{provinceCode:int}")]
    public async Task<IActionResult> GetDistricts(int provinceCode)
    {
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

    /// <summary>
    /// Get wards by district code
    /// GET /api/address/wards/{districtCode}
    /// Returns wards for the specified district
    /// </summary>
    [HttpGet("wards/{districtCode:int}")]
    public async Task<IActionResult> GetWards(int districtCode)
    {
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
