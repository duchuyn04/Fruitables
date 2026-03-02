using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Controllers;

/// <summary>
/// Controller for user authentication (register, login, logout)
/// </summary>
public class AccountController : Controller
{
    private readonly IUserAuthService _userAuthService;
    private readonly IGoogleAuthService _googleAuthService;

    public AccountController(IUserAuthService userAuthService, IGoogleAuthService googleAuthService)
    {
        _userAuthService = userAuthService;
        _googleAuthService = googleAuthService;
    }

    /// <summary>
    /// GET: /Account/Register - Display registration form
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }
        return View(new RegisterRequest());
    }

    /// <summary>
    /// POST: /Account/Register - Process registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userAuthService.RegisterAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }


    /// <summary>
    /// GET: /Account/Login - Display login form
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginRequest());
    }

    /// <summary>
    /// POST: /Account/Login - Process login
    /// Giỏ hàng được merge từ localStorage bằng JavaScript
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var result = await _userAuthService.LoginAsync(model.Email, model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng nhập thất bại");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var user = result.User!;
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        TempData["SuccessMessage"] = $"Chào mừng {user.Name}! Đăng nhập thành công.";

        // Redirect based on returnUrl or role
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var redirectUrl = _userAuthService.GetRedirectUrlByRole(user.Role);
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// POST: /Account/Logout - Process logout
    /// Giỏ hàng được lưu trên localStorage nên không bị mất
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// GET: /Account/GoogleLogin - Redirect to Google for authentication
    /// </summary>
    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        if (!_googleAuthService.IsGoogleAuthEnabled())
        {
            TempData["ErrorMessage"] = "Đăng nhập Google chưa được cấu hình";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// GET: /Account/GoogleCallback - Handle Google OAuth callback
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Đăng nhập bị hủy";
            return RedirectToAction(nameof(Login));
        }

        var claims = authenticateResult.Principal?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
        {
            TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google";
            return RedirectToAction(nameof(Login));
        }

        var result = await _googleAuthService.ProcessGoogleLoginAsync(email, name, googleId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Login));
        }

        var user = result.User!;
        
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        TempData["SuccessMessage"] = $"Chào mừng {user.Name}! Đăng nhập thành công.";

        // Redirect based on returnUrl or role
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Redirect theo role (Admin/SuperAdmin -> Admin Dashboard, Customer -> Home)
        var redirectUrl = _userAuthService.GetRedirectUrlByRole(user.Role);
        return Redirect(redirectUrl);
    }

    private IActionResult RedirectToHome()
    {
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// GET: /Account/ForgotPassword - Display forgot password form
    /// </summary>
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View(new ForgotPasswordRequest());
    }

    /// <summary>
    /// POST: /Account/ForgotPassword - Send reset email
    /// Always shows success message (security: don't reveal if email exists)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resetCallbackUrl = Url.Action(nameof(ResetPassword), "Account",
            values: null, protocol: Request.Scheme)!;

        await _userAuthService.GeneratePasswordResetTokenAsync(model.Email, resetCallbackUrl);

        // Always show success to prevent email enumeration
        TempData["SuccessMessage"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư của bạn.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    /// <summary>
    /// GET: /Account/ResetPassword?email=xx&token=yy - Display reset password form
    /// Validates token before showing the form
    /// </summary>
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordRequest { Email = email, Token = token });
    }

    /// <summary>
    /// POST: /Account/ResetPassword - Process password reset
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var success = await _userAuthService.ResetPasswordAsync(model);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }
}
