using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Fruitables.ViewModels
{
    /// <summary>
    /// ViewModel for General Settings (Site Name, Logo, Favicon)
    /// </summary>
    public class GeneralSettingsViewModel
    {
        [Display(Name = "Site Name")]
        public string? SiteName { get; set; }

        [Display(Name = "Logo Path")]
        public string? LogoPath { get; set; }

        [Display(Name = "Favicon Path")]
        public string? FaviconPath { get; set; }

        [Display(Name = "Logo File")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Favicon File")]
        public IFormFile? FaviconFile { get; set; }
    }

    /// <summary>
    /// ViewModel for SEO Settings
    /// </summary>
    public class SeoSettingsViewModel
    {
        [Display(Name = "Meta Title")]
        [MaxLength(60, ErrorMessage = "Meta Title should not exceed 60 characters")]
        public string? MetaTitle { get; set; }

        [Display(Name = "Meta Description")]
        [MaxLength(160, ErrorMessage = "Meta Description should not exceed 160 characters")]
        public string? MetaDescription { get; set; }

        [Display(Name = "Meta Keywords")]
        public string? MetaKeywords { get; set; }

        [Display(Name = "Google Analytics ID")]
        public string? GoogleAnalyticsId { get; set; }
    }


    /// <summary>
    /// ViewModel for Contact Settings
    /// </summary>
    public class ContactSettingsViewModel
    {
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Display(Name = "Working Hours")]
        public string? WorkingHours { get; set; }

        [Display(Name = "Map Embed Code")]
        public string? MapEmbedCode { get; set; }
    }

    /// <summary>
    /// ViewModel for Social Media Settings
    /// </summary>
    public class SocialSettingsViewModel
    {
        [Display(Name = "Facebook URL")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Facebook { get; set; }

        [Display(Name = "Twitter URL")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Twitter { get; set; }

        [Display(Name = "Instagram URL")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Instagram { get; set; }

        [Display(Name = "Youtube URL")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Youtube { get; set; }

        [Display(Name = "LinkedIn URL")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? LinkedIn { get; set; }
    }

    /// <summary>
    /// ViewModel for a single banner item
    /// </summary>
    public class BannerItemViewModel
    {
        [Display(Name = "Tiêu đề")]
        [MaxLength(100)]
        public string? Title { get; set; }

        [Display(Name = "Phụ đề")]
        [MaxLength(100)]
        public string? Subtitle { get; set; }

        [Display(Name = "Đường dẫn hình ảnh")]
        public string? ImagePath { get; set; }

        [Display(Name = "Hình ảnh")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Link")]
        public string? Link { get; set; }
    }

    /// <summary>
    /// ViewModel for Banner Settings (3 feature banners)
    /// </summary>
    public class BannerSettingsViewModel
    {
        public BannerItemViewModel Banner1 { get; set; } = new();
        public BannerItemViewModel Banner2 { get; set; } = new();
        public BannerItemViewModel Banner3 { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for Shipping Settings
    /// Requirements 1.1: Cấu hình phí vận chuyển theo khu vực
    /// </summary>
    public class ShippingSettingsViewModel
    {
        [Display(Name = "Phí ship Nội thành (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí ship phải >= 0")]
        public decimal FeeZone1 { get; set; } = 30000;

        [Display(Name = "Phí ship Ngoại thành (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí ship phải >= 0")]
        public decimal FeeZone2 { get; set; } = 40000;

        [Display(Name = "Phí ship Vùng xa (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí ship phải >= 0")]
        public decimal FeeZone3 { get; set; } = 50000;

        [Display(Name = "Ngưỡng miễn phí ship (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Ngưỡng phải >= 0")]
        public decimal FreeShippingThreshold { get; set; } = 500000;

        [Display(Name = "Phí giảm Vùng xa khi đạt ngưỡng (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí giảm phải >= 0")]
        public decimal ReducedFeeZone3 { get; set; } = 20000;

        [Display(Name = "Quận/huyện Khu vực 1 (Nội thành)")]
        public List<string> Zone1Districts { get; set; } = new();

        [Display(Name = "Quận/huyện Khu vực 2 (Ngoại thành)")]
        public List<string> Zone2Districts { get; set; } = new();

        /// <summary>
        /// Danh sách tất cả quận/huyện TP.HCM để hiển thị trong multi-select
        /// </summary>
        public List<string> AllDistricts { get; set; } = new();
    }
}
