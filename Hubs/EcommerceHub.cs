using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fruitables.Hubs
{
    [Authorize]
    public class EcommerceHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Join user-specific group
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User:{userId}");
            }

            // Join Admins group if user is Admin or SuperAdmin
            if (Context.User != null && (Context.User.IsInRole("Admin") || Context.User.IsInRole("SuperAdmin")))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinOrderGroup(int orderId, [Microsoft.AspNetCore.Mvc.FromServices] Fruitables.Data.ApplicationDbContext dbContext)
        {
            // Optional: verify if user owns the order or is admin
            if (Context.User != null && (Context.User.IsInRole("Admin") || Context.User.IsInRole("SuperAdmin")))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Order:{orderId}");
                return;
            }

            // Customer
            var userIdStr = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var orderExists = await dbContext.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId);
                if (orderExists)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Order:{orderId}");
                    return;
                }
            }
            
            throw new HubException("Unauthorized to join this order group.");
        }

        public async Task LeaveOrderGroup(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order:{orderId}");
        }

        [AllowAnonymous]
        public async Task JoinProductGroup(int productId)
        {
            // Anyone can join product group to see stock updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Product:{productId}");
        }

        [AllowAnonymous]
        public async Task LeaveProductGroup(int productId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Product:{productId}");
        }
    }
}
