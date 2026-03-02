using System.ComponentModel.DataAnnotations;

namespace Fruitables.Areas.Admin.ViewModels;

// Gom các ViewModel liên quan đến cấu hình hệ thống

// Cấu hình SMTP để gửi email (Quên mật khẩu, Thông báo tài khoản...)
public class SmtpSettingsViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập SMTP Host")]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Port")]
    [Range(1, 65535, ErrorMessage = "Port không hợp lệ")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool EnableSsl { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng nhập tên người gửi")]
    public string SenderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email người gửi")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string SenderEmail { get; set; } = string.Empty;
}

// Cấu hình Google OAuth để đăng nhập bằng tài khoản Google
public class GoogleAuthSettingsViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Client ID")]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public bool IsEnabled { get; set; } = true;
}

// Cấu hình thông tin cơ bản của website
public class GeneralSettingsViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên website")]
    public string SiteName { get; set; } = string.Empty;

    public string? SupportEmail { get; set; }

    public string? SupportPhone { get; set; }

    public string? SiteDescription { get; set; }
}

// ViewModel tổng hợp để truyền cả 3 nhóm cài đặt vào View
public class SettingsIndexViewModel
{
    public SmtpSettingsViewModel Smtp { get; set; } = new();
    public GoogleAuthSettingsViewModel GoogleAuth { get; set; } = new();
    public GeneralSettingsViewModel General { get; set; } = new();
}
