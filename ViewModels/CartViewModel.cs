using Fruitables.Models;

namespace Fruitables.ViewModels;

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public decimal Discount { get; set; }
    
    /// <summary>
    /// Thông tin phí vận chuyển chi tiết (theo khu vực)
    /// Requirements 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public ShippingInfo? ShippingInfo { get; set; }
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
    public decimal Total => Price * Quantity;
}
