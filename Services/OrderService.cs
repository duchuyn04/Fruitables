using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using Fruitables.Helpers;

namespace Fruitables.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;

    public OrderService(IUnitOfWork unitOfWork, ICartService cartService)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
    }

    public async Task<Order> CreateOrderAsync(CheckoutViewModel model, string sessionId, int? userId = null)
    {
        var cart = await _cartService.GetCartAsync(sessionId);
        
        // Use shipping fee from model.Cart if available (snapshot from checkout), otherwise from fresh cart
        var shippingFee = model.Cart?.ShippingFee ?? cart.ShippingFee;

        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            Subtotal = cart.Subtotal,
            ShippingFee = shippingFee, // Use snapshot shipping fee (Requirements 6.3, 8.1, 8.2)
            Discount = cart.Discount,
            Total = cart.Subtotal + shippingFee - cart.Discount,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            ShippingMethod = model.ShippingMethod,
            Notes = model.Notes
        };

        // Add order items and deduct stock
        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Total = item.Total
            });

            // Deduct stock immediately when order is placed
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
            }
        }

        // Handle address - either use selected address or create new one
        Address? shippingAddress = null;
        
        if (model.SelectedAddressId.HasValue)
        {
            // Use existing address
            shippingAddress = await _unitOfWork.Addresses.GetByIdAsync(model.SelectedAddressId.Value);
            order.AddressId = model.SelectedAddressId.Value;
        }
        else if (!string.IsNullOrEmpty(model.StreetAddress))
        {
            // Create new address
            var fullName = model.FullName ?? model.FirstName.Trim();
            shippingAddress = new Address
            {
                UserId = userId,
                FullName = fullName,
                Phone = model.Mobile,
                ProvinceCode = model.ProvinceCode,
                ProvinceName = model.ProvinceName ?? string.Empty,
                DistrictCode = model.DistrictCode,
                DistrictName = model.DistrictName ?? string.Empty,
                WardCode = model.WardCode,
                WardName = model.WardName ?? string.Empty,
                StreetAddress = model.StreetAddress,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Addresses.AddAsync(shippingAddress);
            await _unitOfWork.SaveChangesAsync(); // Save to get ID
            
            order.AddressId = shippingAddress.Id;
        }

        // Create snapshot of shipping address
        if (shippingAddress != null)
        {
            order.ShippingSnapshot = AddressSnapshotHelper.ToSnapshot(shippingAddress);
        }

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Clear cart after order
        await _cartService.ClearCartAsync(sessionId);

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _unitOfWork.Orders.Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        return await _unitOfWork.Orders.Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
    {
        return await _unitOfWork.Orders.Query()
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<Address?> GetShippingAddressFromSnapshotAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.ShippingSnapshot))
            return null;

        return AddressSnapshotHelper.FromSnapshot(order.ShippingSnapshot);
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static decimal GetShippingFee(ShippingMethod method)
    {
        return method switch
        {
            ShippingMethod.Free => 0,
            ShippingMethod.FlatRate => 15.00m,
            ShippingMethod.LocalPickup => 8.00m,
            _ => 15.00m
        };
    }
}
