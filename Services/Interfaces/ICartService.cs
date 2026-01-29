using Fruitables.ViewModels;

namespace Fruitables.Services.Interfaces;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync(string sessionId, string? district = null);
    Task AddToCartAsync(string sessionId, int productId, int quantity = 1);
    Task UpdateQuantityAsync(string sessionId, int productId, int quantity);
    Task RemoveFromCartAsync(string sessionId, int productId);
    Task ClearCartAsync(string sessionId);
    Task<int> GetCartCountAsync(string sessionId);
    Task ApplyCouponAsync(string sessionId, string couponCode);
}
