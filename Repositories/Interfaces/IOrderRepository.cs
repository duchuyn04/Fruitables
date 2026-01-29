using Fruitables.Models;
using Fruitables.ViewModels;

namespace Fruitables.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Lấy danh sách đơn hàng của khách hàng với phân trang và lọc
    /// </summary>
    /// <param name="userId">ID khách hàng</param>
    /// <param name="filter">Bộ lọc và tham số phân trang</param>
    /// <returns>Kết quả phân trang chứa danh sách đơn hàng</returns>
    Task<PagedResult<Order>> GetOrdersByUserIdAsync(int userId, OrderHistoryFilter filter);

    /// <summary>
    /// Lấy đơn hàng với đầy đủ thông tin chi tiết
    /// </summary>
    /// <param name="orderId">ID đơn hàng</param>
    /// <param name="userId">ID khách hàng (để kiểm tra quyền truy cập)</param>
    /// <returns>Đơn hàng với thông tin chi tiết hoặc null</returns>
    Task<Order?> GetOrderWithDetailsAsync(int orderId, int userId);

    /// <summary>
    /// Lấy lịch sử thay đổi trạng thái của đơn hàng
    /// </summary>
    /// <param name="orderId">ID đơn hàng</param>
    /// <returns>Danh sách lịch sử thay đổi trạng thái</returns>
    Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId);

    /// <summary>
    /// Cập nhật đơn hàng với lý do hủy
    /// </summary>
    /// <param name="orderId">ID đơn hàng</param>
    /// <param name="status">Trạng thái mới</param>
    /// <param name="cancelReason">Lý do hủy (nếu có)</param>
    /// <param name="userId">ID người thực hiện thao tác</param>
    /// <returns>True nếu cập nhật thành công</returns>
    Task<bool> UpdateOrderStatusWithReasonAsync(int orderId, OrderStatus status, string? cancelReason = null, int? userId = null);

    /// <summary>
    /// Hủy đơn hàng và hoàn trả stock trong một transaction
    /// </summary>
    /// <param name="orderId">ID đơn hàng</param>
    /// <param name="cancelReason">Lý do hủy</param>
    /// <param name="userId">ID người thực hiện thao tác</param>
    /// <returns>Kết quả hoàn trả stock</returns>
    Task<StockRestoreResult> CancelOrderWithStockRestoreAsync(int orderId, string cancelReason, int? userId = null);
}