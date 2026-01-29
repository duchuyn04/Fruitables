using Fruitables.Models;
using Fruitables.ViewModels;

namespace Fruitables.Services.Interfaces;

/// <summary>
/// Interface for user authentication service (registration, login, logout)
/// </summary>
public interface IUserAuthService
{
    /// <summary>
    /// Register a new user account
    /// </summary>
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Login with email and password
    /// </summary>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>
    /// Logout current user
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Get redirect URL based on user role
    /// Customer -> /Home/Index
    /// Admin/SuperAdmin -> /Admin/Dashboard
    /// </summary>
    string GetRedirectUrlByRole(UserRole role);

    /// <summary>
    /// Hash password using BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify password against BCrypt hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
