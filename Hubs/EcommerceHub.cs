using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fruitables.Hubs
{
    public class EcommerceHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                // Join user-specific group
                var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User:{userId}");
                }

                // Join Admins group if user is Admin or SuperAdmin
                if (Context.User.IsInRole("Admin") || Context.User.IsInRole("SuperAdmin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinOrderGroup(int orderId, [Microsoft.AspNetCore.Mvc.FromServices] Fruitables.Data.ApplicationDbContext dbContext)
        {
            if (orderId <= 0) throw new HubException("Invalid orderId.");

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
            if (orderId <= 0) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order:{orderId}");
        }

        public async Task JoinProductGroup(int productId)
        {
            if (productId <= 0) throw new HubException("Invalid productId.");
            // Anyone can join product group to see stock updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Product:{productId}");
        }

        public async Task LeaveProductGroup(int productId)
        {
            if (productId <= 0) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Product:{productId}");
        }
    }
}
