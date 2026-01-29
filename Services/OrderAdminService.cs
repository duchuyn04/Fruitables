using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Fruitables.Data;
using Fruitables.Models;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Services;

public class OrderAdminService : IOrderAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderLogService _logService;
    private readonly IWebHostEnvironment _environment;

    // Valid file extensions for audit log attachments (Requirements: 10.2)
    private static readonly string[] ValidAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly string[] ValidAttachmentContentTypes = 
    { 
        "image/jpeg", 
        "image/jpg", 
        "image/png", 
        "application/pdf" 
    };
    
    // Max file size: 5MB (Requirements: 10.2)
    private const long MaxAttachmentFileSize = 5 * 1024 * 1024;

    public OrderAdminService(ApplicationDbContext context, IOrderLogService logService, IWebHostEnvironment environment)
    {
        _context = context;
        _logService = logService;
        _environment = environment;
    }

    public async Task<OrderListResult> GetOrdersAsync(OrderListRequest request)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(o => o.OrderNumber.Contains(request.Search));
        }

        // Filter by Status
        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        // Filter by PaymentStatus
        if (request.PaymentStatus.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);
        }

        // Filter by Date Range
        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Sorting
        query = request.SortBy?.ToLower() switch
        {
            "total" => request.SortDescending
                ? query.OrderByDescending(o => o.Total)
                : query.OrderBy(o => o.Total),
            "status" => request.SortDescending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => request.SortDescending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };

        // Pagination
        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new OrderListResult
        {
            Orders = orders,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Include(o => o.Address)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderWithHistoryAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.User)
            .Include(o => o.Address)
            .Include(o => o.StatusHistory)
                .ThenInclude(h => h.Admin)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrderResult> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
    {
        try
        {
            // Get order with items
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
                return OrderResult.Fail(OrderErrorType.NotFound, "Đơn hàng không tồn tại");

            // Validate status transition
            if (!IsValidStatusTransition(order.Status, request.NewStatus))
                return OrderResult.Fail(OrderErrorType.InvalidStatusTransition, 
                    $"Không thể chuyển từ {order.Status} sang {request.NewStatus}");

            // Handle stock management
            // Stock is deducted when order is placed (in OrderService.CreateOrderAsync)
            // So we only need to restore stock when cancelling, and deduct when restoring from cancelled
            if (request.NewStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                // Restore stock when cancelling (stock was deducted when order was placed)
                await RestoreStockForOrder(order);
            }
            else if (order.Status == OrderStatus.Cancelled && request.NewStatus != OrderStatus.Cancelled)
            {
                // Deduct stock when restoring from cancelled
                var insufficientItems = await CheckAndDeductStockForOrder(order);
                if (insufficientItems.Any())
                    return OrderResult.FailWithInsufficientStock(insufficientItems);
            }

            // Update order status
            var oldStatus = order.Status;
            order.Status = request.NewStatus;
            await _context.SaveChangesAsync();

            // Log the change
            await _logService.LogStatusChangeAsync(order.Id, oldStatus, request.NewStatus, request.AdminId, request.Notes);

            return OrderResult.Ok(order);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OrderResult.Fail(OrderErrorType.ConcurrencyConflict, 
                "Đơn hàng đã được cập nhật bởi người dùng khác. Vui lòng tải lại dữ liệu.");
        }
    }

    private async Task RestoreStockForOrder(Order order)
    {
        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task<List<InsufficientStockItem>> CheckAndDeductStockForOrder(Order order)
    {
        var insufficientItems = new List<InsufficientStockItem>();

        // First check if all items have sufficient stock
        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
            {
                insufficientItems.Add(new InsufficientStockItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = product?.StockQuantity ?? 0
                });
            }
        }

        // If all items have sufficient stock, deduct them
        if (!insufficientItems.Any())
        {
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }

        return insufficientItems;
    }

    public async Task<OrderResult> UpdatePaymentStatusAsync(UpdatePaymentStatusRequest request)
    {
        try
        {
            var order = await _context.Orders.FindAsync(request.OrderId);

            if (order == null)
                return OrderResult.Fail(OrderErrorType.NotFound, "Đơn hàng không tồn tại");

            // Validate payment status transition
            if (!IsValidPaymentStatusTransition(order.PaymentStatus, request.NewPaymentStatus, order.Status))
                return OrderResult.Fail(OrderErrorType.InvalidPaymentStatusTransition,
                    $"Không thể chuyển trạng thái thanh toán từ {order.PaymentStatus} sang {request.NewPaymentStatus}");

            // Update payment status
            var oldPaymentStatus = order.PaymentStatus;
            order.PaymentStatus = request.NewPaymentStatus;
            await _context.SaveChangesAsync();

            // Log the change
            await _logService.LogPaymentStatusChangeAsync(order.Id, oldPaymentStatus, request.NewPaymentStatus, request.AdminId, request.Notes);

            return OrderResult.Ok(order);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OrderResult.Fail(OrderErrorType.ConcurrencyConflict,
                "Đơn hàng đã được cập nhật bởi người dùng khác. Vui lòng tải lại dữ liệu.");
        }
    }

    public bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        // Cannot transition to same status
        if (currentStatus == newStatus)
            return false;

        // Define valid transitions as a state machine
        return (currentStatus, newStatus) switch
        {
            // From Pending
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,

            // From Processing
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,

            // From Shipped
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Shipped, OrderStatus.Cancelled) => true,

            // From Delivered - can cancel (for returns/refunds)
            (OrderStatus.Delivered, OrderStatus.Cancelled) => true,

            // From Cancelled - can restore to Processing
            (OrderStatus.Cancelled, OrderStatus.Processing) => true,

            // All other transitions are invalid
            _ => false
        };
    }

    public bool IsValidPaymentStatusTransition(PaymentStatus currentStatus, PaymentStatus newStatus, OrderStatus orderStatus)
    {
        // Cannot transition to same status
        if (currentStatus == newStatus)
            return false;

        // Refund is only allowed when order is Cancelled
        if (newStatus == PaymentStatus.Refunded && orderStatus != OrderStatus.Cancelled)
            return false;

        // Define valid payment transitions
        return (currentStatus, newStatus) switch
        {
            // From Pending
            (PaymentStatus.Pending, PaymentStatus.Paid) => true,

            // From Paid
            (PaymentStatus.Paid, PaymentStatus.Refunded) => orderStatus == OrderStatus.Cancelled,

            // All other transitions are invalid
            _ => false
        };
    }

    /// <summary>
    /// Validates if a state transition is valid according to the state transition matrix
    /// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5
    /// - 6.1: Delivered cannot go back to Pending, Processing, Shipped
    /// - 6.2: Cancelled is terminal - no transitions allowed
    /// - 6.3: Returned is terminal - no transitions allowed
    /// - 6.4: Shipped cannot go back to Pending, Processing
    /// - 6.5: Processing cannot go back to Pending
    /// </summary>
    public bool IsValidStateTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return StateTransitionRules.IsValidTransition(currentStatus, newStatus);
    }

    /// <summary>
    /// Gets the state transition rule for a specific order status
    /// Requirements: 6.6
    /// </summary>
    public StateTransitionRule GetStateTransitionRule(OrderStatus currentStatus)
    {
        return StateTransitionRules.GetRule(currentStatus);
    }

    /// <summary>
    /// Gets the allowed transitions for a specific order status
    /// Requirements: 6.6
    /// </summary>
    public OrderStatus[] GetAllowedTransitions(OrderStatus currentStatus)
    {
        return StateTransitionRules.GetAllowedTransitions(currentStatus);
    }

    /// <summary>
    /// Validates if a status combination is valid according to business rules (legacy - uses COD rules)
    /// </summary>
    public bool IsValidStatusCombination(OrderStatus orderStatus, PaymentStatus paymentStatus)
    {
        return StatusCombinationRules.IsValidCombination(orderStatus, paymentStatus);
    }

    /// <summary>
    /// Gets the combination rule for a specific order status (legacy - uses COD rules)
    /// </summary>
    public StatusCombinationRule GetStatusCombinationRule(OrderStatus orderStatus)
    {
        return StatusCombinationRules.GetRule(orderStatus);
    }

    /// <summary>
    /// Gets the allowed payment statuses for a specific order status (legacy - uses COD rules)
    /// </summary>
    public PaymentStatus[] GetAllowedPaymentStatuses(OrderStatus orderStatus)
    {
        return StatusCombinationRules.GetAllowedPaymentStatuses(orderStatus);
    }

    /// <summary>
    /// Validates if a status combination is valid according to business rules (payment method aware)
    /// Requirements: 3.2, 3.3, 7.1, 7.2, 7.3
    /// - 3.2: COD orders have specific valid combinations
    /// - 3.3: Bank Transfer orders allow Paid before Delivered
    /// - 7.1: COD orders lock PaymentStatus to Pending for Pending/Processing/Shipped
    /// - 7.2: Bank Transfer orders allow Pending or Paid for Pending/Processing/Shipped
    /// - 7.3: Bank Transfer Cancelled orders only allow Pending or Refunded
    /// </summary>
    public bool IsValidStatusCombination(OrderStatus orderStatus, PaymentStatus paymentStatus, PaymentMethod paymentMethod)
    {
        return StatusCombinationRules.IsValidCombination(orderStatus, paymentStatus, paymentMethod);
    }

    /// <summary>
    /// Gets the combination rule for a specific order status and payment method
    /// Requirements: 7.1, 7.2, 7.3
    /// </summary>
    public StatusCombinationRule GetStatusCombinationRule(OrderStatus orderStatus, PaymentMethod paymentMethod)
    {
        return StatusCombinationRules.GetRule(orderStatus, paymentMethod);
    }

    /// <summary>
    /// Gets the allowed payment statuses for a specific order status and payment method
    /// Requirements: 7.1, 7.2, 7.3
    /// </summary>
    public PaymentStatus[] GetAllowedPaymentStatuses(OrderStatus orderStatus, PaymentMethod paymentMethod)
    {
        return StatusCombinationRules.GetAllowedPaymentStatuses(orderStatus, paymentMethod);
    }

    /// <summary>
    /// Updates both order status and payment status in a single transaction
    /// Requirements: 1.2, 3.1, 3.4, 9.1
    /// - 1.2: Combined update in a single transaction
    /// - 3.1: Validate status combination according to payment method
    /// - 3.4: Validation failure does not change database
    /// - 9.1: Create audit log entry with complete information
    /// </summary>
    /// <param name="request">The update request containing new statuses and notes</param>
    /// <param name="adminId">The ID of the admin performing the update</param>
    /// <returns>OrderResult indicating success or failure</returns>
    public async Task<OrderResult> UpdateCombinedStatusAsync(UpdateCombinedStatusRequest request, int adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get order with items
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return OrderResult.Fail(OrderErrorType.NotFound, "Đơn hàng không tồn tại");
            }

            var oldOrderStatus = order.Status;
            var oldPaymentStatus = order.PaymentStatus;

            // Validate state transition (Requirements: 6.1, 6.2, 6.3, 6.4, 6.5)
            if (!IsValidStateTransition(oldOrderStatus, request.NewOrderStatus))
            {
                return OrderResult.Fail(OrderErrorType.InvalidStatusTransition,
                    $"Không thể chuyển từ trạng thái {oldOrderStatus} sang {request.NewOrderStatus}");
            }

            // Validate status combination with payment method awareness (Requirements: 3.2, 3.3, 7.1, 7.2, 7.3)
            if (!IsValidStatusCombination(request.NewOrderStatus, request.NewPaymentStatus, order.PaymentMethod))
            {
                return OrderResult.Fail(OrderErrorType.ValidationError,
                    $"Tổ hợp trạng thái không hợp lệ: {request.NewOrderStatus} + {request.NewPaymentStatus} cho phương thức thanh toán {order.PaymentMethod}");
            }

            // Handle stock management for order status changes
            if (request.NewOrderStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                // Restore stock when cancelling
                await RestoreStockForOrder(order);
            }
            else if (request.NewOrderStatus == OrderStatus.Returned && order.Status != OrderStatus.Returned)
            {
                // Restore stock when returning (if not already cancelled)
                if (order.Status != OrderStatus.Cancelled)
                {
                    await RestoreStockForOrder(order);
                }
            }
            else if (order.Status == OrderStatus.Cancelled && request.NewOrderStatus != OrderStatus.Cancelled)
            {
                // Deduct stock when restoring from cancelled
                var insufficientItems = await CheckAndDeductStockForOrder(order);
                if (insufficientItems.Any())
                {
                    await transaction.RollbackAsync();
                    return OrderResult.FailWithInsufficientStock(insufficientItems);
                }
            }

            // Update both statuses atomically (Requirement 1.2)
            order.Status = request.NewOrderStatus;
            order.PaymentStatus = request.NewPaymentStatus;

            await _context.SaveChangesAsync();

            // Get admin info for audit log (Requirement 9.1)
            var admin = await _context.Users.FindAsync(adminId);
            var adminName = admin?.Name ?? "Unknown";
            var adminEmail = admin?.Email ?? "unknown@unknown.com";

            // Create audit log entry (Requirement 9.1)
            var auditLog = new OrderStatusAuditLog
            {
                OrderId = order.Id,
                AdminId = adminId,
                AdminName = adminName,
                AdminEmail = adminEmail,
                CreatedAt = DateTime.UtcNow,
                OldOrderStatus = oldOrderStatus,
                NewOrderStatus = request.NewOrderStatus,
                OldPaymentStatus = oldPaymentStatus,
                NewPaymentStatus = request.NewPaymentStatus,
                Notes = request.Notes
            };

            _context.OrderStatusAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Also log to existing OrderStatusHistory for backward compatibility
            if (oldOrderStatus != request.NewOrderStatus)
            {
                await _logService.LogStatusChangeAsync(order.Id, oldOrderStatus, request.NewOrderStatus, adminId, request.Notes);
            }

            if (oldPaymentStatus != request.NewPaymentStatus)
            {
                await _logService.LogPaymentStatusChangeAsync(order.Id, oldPaymentStatus, request.NewPaymentStatus, adminId, request.Notes);
            }

            await transaction.CommitAsync();
            return OrderResult.Ok(order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return OrderResult.Fail(OrderErrorType.ConcurrencyConflict,
                "Đơn hàng đã được cập nhật bởi người dùng khác. Vui lòng tải lại dữ liệu.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets audit logs for an order, sorted by most recent first
    /// Requirements: 9.2, 9.3
    /// </summary>
    public async Task<List<OrderStatusAuditLog>> GetAuditLogsAsync(int orderId)
    {
        return await _context.OrderStatusAuditLogs
            .Include(a => a.Attachments)
            .Where(a => a.OrderId == orderId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Validates if a file is a valid attachment type (JPG, PNG, PDF)
    /// Requirements: 10.2
    /// </summary>
    public bool IsValidAttachmentFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ValidAttachmentExtensions.Contains(extension))
            return false;

        // Check content type
        if (!ValidAttachmentContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }

    /// <summary>
    /// Validates if a file size is within the allowed limit (5MB)
    /// Requirements: 10.2
    /// </summary>
    public bool IsValidAttachmentFileSize(IFormFile file)
    {
        if (file == null)
            return false;

        return file.Length <= MaxAttachmentFileSize;
    }

    /// <summary>
    /// Saves an attachment file and creates an AuditLogAttachment record
    /// Requirements: 10.2, 10.3
    /// - 10.2: Validate file type (JPG, PNG, PDF) and size (max 5MB)
    /// - 10.3: Save file and link to audit log entry
    /// </summary>
    public async Task<AuditLogAttachment> SaveAuditLogAttachmentAsync(int auditLogId, IFormFile file)
    {
        // Validate file type (Requirements: 10.2)
        if (!IsValidAttachmentFile(file))
        {
            throw new InvalidOperationException("File không phải định dạng hợp lệ. Chỉ chấp nhận: JPG, PNG, PDF");
        }

        // Validate file size (Requirements: 10.2)
        if (!IsValidAttachmentFileSize(file))
        {
            throw new InvalidOperationException("File vượt quá kích thước cho phép (5MB)");
        }

        // Verify audit log exists
        var auditLog = await _context.OrderStatusAuditLogs.FindAsync(auditLogId);
        if (auditLog == null)
        {
            throw new InvalidOperationException("Audit log không tồn tại");
        }

        // Create upload directory if not exists
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "audit-attachments");
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = Path.Combine(uploadPath, uniqueFileName);

        // Save file to disk
        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create attachment record (Requirements: 10.3)
        var attachment = new AuditLogAttachment
        {
            AuditLogId = auditLogId,
            FileName = file.FileName,
            FilePath = $"/uploads/audit-attachments/{uniqueFileName}",
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _context.AuditLogAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return attachment;
    }
}
