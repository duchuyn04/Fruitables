using Fruitables.Models;

namespace Fruitables.ViewModels;

public enum GoogleAuthErrorType
{
    EmailNotProvided,
    AccountLocked,
    AuthenticationFailed,
    AuthenticationCancelled
}

public class GoogleAuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public GoogleAuthErrorType? ErrorType { get; set; }

    public static GoogleAuthResult Succeeded(User user)
    {
        return new GoogleAuthResult
        {
            Success = true,
            User = user
        };
    }

    public static GoogleAuthResult Failed(GoogleAuthErrorType errorType, string message)
    {
        return new GoogleAuthResult
        {
            Success = false,
            ErrorType = errorType,
            ErrorMessage = message
        };
    }
}
