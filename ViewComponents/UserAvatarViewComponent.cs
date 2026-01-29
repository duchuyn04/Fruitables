using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Fruitables.Data;

namespace Fruitables.ViewComponents;

/// <summary>
/// ViewComponent để hiển thị avatar của user hiện tại
/// </summary>
public class UserAvatarViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private const string DefaultAvatarUrl = "/img/default-avatar.svg";

    public UserAvatarViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var avatarUrl = DefaultAvatarUrl;

        if (UserClaimsPrincipal?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Avatar })
                    .FirstOrDefaultAsync();

                if (user != null && !string.IsNullOrEmpty(user.Avatar))
                {
                    avatarUrl = user.Avatar;
                }
            }
        }

        return View("Default", avatarUrl);
    }
}
