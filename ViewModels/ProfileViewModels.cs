using System.ComponentModel.DataAnnotations;

namespace Fruitables.ViewModels;

/// <summary>
/// ViewModel để hiển thị thông tin profile của user
/// </summary>
public class ProfileViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string AvatarUrl { get; set; } = "/img/default-avatar.svg"; // URL hoặc default avatar
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model để cập nhật thông tin profile
/// </summary>
public class UpdateProfileRequest
{
    public int UserId { get; set; }
    
    [Required(ErrorMessage = "Tên không được để trống")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Tên phải từ 1-200 ký tự")]
    public string Name { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Số điện thoại phải từ 10-20 ký tự")]
    public string? Phone { get; set; }
}

/// <summary>
/// Result model cho các operations của ProfileService
/// </summary>
public class ProfileResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ProfileErrorType? ErrorType { get; set; }
    public ProfileViewModel? Profile { get; set; }
    
    public static ProfileResult Ok(ProfileViewModel profile)
    {
        return new ProfileResult
        {
            Success = true,
            Profile = profile
        };
    }
    
    public static ProfileResult Fail(ProfileErrorType type, string message)
    {
        return new ProfileResult
        {
            Success = false,
            ErrorType = type,
            ErrorMessage = message
        };
    }
}

/// <summary>
/// Enum định nghĩa các loại lỗi có thể xảy ra trong ProfileService
/// </summary>
public enum ProfileErrorType
{
    NotFound,
    ValidationError,
    InvalidFileType,
    FileTooLarge,
    UploadFailed
}