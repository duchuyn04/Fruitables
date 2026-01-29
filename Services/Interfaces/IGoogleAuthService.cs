using Fruitables.Models;
using Fruitables.ViewModels;

namespace Fruitables.Services.Interfaces;

public interface IGoogleAuthService
{
    /// <summary>
    /// Xử lý đăng nhập Google và trả về kết quả
    /// </summary>
    /// <param name="email">Email từ Google</param>
    /// <param name="name">Tên đầy đủ từ Google</param>
    /// <param name="googleId">ID duy nhất từ Google</param>
    /// <returns>Kết quả đăng nhập Google</returns>
    Task<GoogleAuthResult> ProcessGoogleLoginAsync(string email, string? name, string googleId);

    /// <summary>
    /// Kiểm tra xem Google Auth có được cấu hình không
    /// Đọc từ Environment Variables: Authentication__Google__ClientId và Authentication__Google__ClientSecret
    /// </summary>
    /// <returns>True nếu cả ClientId và ClientSecret đều được cấu hình</returns>
    bool IsGoogleAuthEnabled();

    /// <summary>
    /// Lấy hoặc tạo user từ thông tin Google
    /// </summary>
    /// <param name="email">Email từ Google</param>
    /// <param name="name">Họ tên từ Google</param>
    /// <param name="googleId">ID duy nhất từ Google</param>
    /// <returns>Tuple chứa User (null nếu bị khóa) và LockReason (nếu có)</returns>
    Task<(User? User, string? LockReason)> GetOrCreateUserFromGoogleAsync(string email, string? name, string googleId);
}
