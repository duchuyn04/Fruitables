using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fruitables.Services;

/// <summary>
/// Service xử lý đăng nhập Google OAuth
/// Đọc credentials từ Environment Variables:
/// - Authentication__Google__ClientId
/// - Authentication__Google__ClientSecret
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool IsGoogleAuthEnabled()
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    /// <inheritdoc />
    public async Task<GoogleAuthResult> ProcessGoogleLoginAsync(string email, string? name, string googleId)
    {
        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            return GoogleAuthResult.Failed(GoogleAuthErrorType.EmailNotProvided, "Không thể lấy email từ Google");
        }

        // Get or create user
        var (user, lockReason) = await GetOrCreateUserFromGoogleAsync(email, name, googleId);
        
        if (user == null)
        {
            // User exists but is locked
            var errorMessage = !string.IsNullOrEmpty(lockReason) 
                ? $"Tài khoản đã bị khóa. Lý do: {lockReason}" 
                : "Tài khoản đã bị khóa";
            return GoogleAuthResult.Failed(GoogleAuthErrorType.AccountLocked, errorMessage);
        }

        // Update LastLoginAt
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return GoogleAuthResult.Succeeded(user);
    }

    /// <inheritdoc />
    public async Task<(User? User, string? LockReason)> GetOrCreateUserFromGoogleAsync(string email, string? name, string googleId)
    {
        var normalizedEmail = email.ToLower();
        var existingUser = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (existingUser != null)
        {
            // Check if temporary lock has expired and auto-unlock
            if (!existingUser.IsActive && 
                existingUser.CurrentLockType == LockType.Temporary && 
                existingUser.LockExpiresAt.HasValue && 
                existingUser.LockExpiresAt.Value <= DateTime.UtcNow)
            {
                // Auto-unlock expired temporary lock
                existingUser.IsActive = true;
                existingUser.CurrentLockType = null;
                existingUser.LockReason = null;
                existingUser.LockViolationType = null;
                existingUser.LockedAt = null;
                existingUser.LockExpiresAt = null;
                existingUser.LockedByAdminId = null;
                await _unitOfWork.SaveChangesAsync();
            }

            // Check if account is locked
            if (!existingUser.IsActive)
            {
                return (null, existingUser.LockReason);
            }

            // Link Google account if not already linked
            if (string.IsNullOrEmpty(existingUser.GoogleId))
            {
                existingUser.GoogleId = googleId;
                existingUser.UpdatedAt = DateTime.UtcNow;
            }

            return (existingUser, null);
        }

        // Create new user
        var newUser = new User
        {
            Email = normalizedEmail,
            Name = name ?? "Google User",
            Password = Guid.NewGuid().ToString(), // Random password for Google users
            GoogleId = googleId,
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(newUser);
        await _unitOfWork.SaveChangesAsync();

        return (newUser, null);
    }
}
