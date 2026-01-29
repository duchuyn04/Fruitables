namespace Fruitables.ViewModels;

/// <summary>
/// ViewModel tổng quan đơn hủy
/// </summary>
public class CancelledOrdersOverviewViewModel
{
    /// <summary>
    /// Tổng số đơn hủy trong khoảng thời gian
    /// </summary>
    public int TotalCancelledOrders { get; set; }

    /// <summary>
    /// Tỷ lệ hủy đơn (số đơn hủy / tổng số đơn × 100%)
    /// </summary>
    public decimal CancellationRate { get; set; }

    /// <summary>
    /// Tổng giá trị đơn hàng bị hủy
    /// </summary>
    public decimal TotalCancelledValue { get; set; }

    /// <summary>
    /// Tổng số đơn trong khoảng thời gian
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Bộ lọc đang áp dụng
    /// </summary>
    public RevenueFilterViewModel Filter { get; set; } = new();
}

/// <summary>
/// ViewModel xu hướng đơn hủy theo thời gian
/// </summary>
public class CancelledOrdersTrendViewModel
{
    /// <summary>
    /// Nhãn thời gian (ngày/tuần/tháng)
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// Số đơn hủy theo từng điểm thời gian
    /// </summary>
    public List<int> CancelledData { get; set; } = new();

    /// <summary>
    /// Tỷ lệ hủy theo từng điểm thời gian
    /// </summary>
    public List<decimal> CancellationRateData { get; set; } = new();

    /// <summary>
    /// Chu kỳ xu hướng (Daily/Weekly/Monthly)
    /// </summary>
    public TrendPeriod Period { get; set; }
}

/// <summary>
/// ViewModel thống kê theo lý do hủy
/// </summary>
public class CancelReasonStatisticsViewModel
{
    /// <summary>
    /// Danh sách các lý do hủy và số lượng
    /// </summary>
    public List<CancelReasonItem> Reasons { get; set; } = new();

    /// <summary>
    /// Tổng số đơn hủy
    /// </summary>
    public int TotalCancelledOrders { get; set; }
}

/// <summary>
/// Item thống kê một lý do hủy
/// </summary>
public class CancelReasonItem
{
    /// <summary>
    /// Lý do hủy (hoặc "Không có lý do" nếu null/empty)
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng đơn hủy với lý do này
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Phần trăm so với tổng số đơn hủy
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// ViewModel so sánh đơn hủy giữa hai kỳ
/// </summary>
public class CancelledOrdersComparisonViewModel
{
    /// <summary>
    /// Số đơn hủy kỳ hiện tại
    /// </summary>
    public int CurrentPeriodCancelled { get; set; }

    /// <summary>
    /// Số đơn hủy kỳ trước
    /// </summary>
    public int PreviousPeriodCancelled { get; set; }

    /// <summary>
    /// Phần trăm thay đổi giữa hai kỳ
    /// </summary>
    public decimal ChangePercent { get; set; }

    /// <summary>
    /// Số lượng thay đổi (current - previous)
    /// </summary>
    public int ChangeAmount { get; set; }

    /// <summary>
    /// Nhãn kỳ hiện tại
    /// </summary>
    public string CurrentPeriodLabel { get; set; } = string.Empty;

    /// <summary>
    /// Nhãn kỳ trước
    /// </summary>
    public string PreviousPeriodLabel { get; set; } = string.Empty;
}

/// <summary>
/// Result type cho các operations thống kê đơn hủy - chứa kết quả hoặc lỗi validation
/// </summary>
public class CancelledOrdersResult<T> where T : class
{
    /// <summary>
    /// Dữ liệu kết quả (null nếu có lỗi validation)
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Có lỗi validation hay không
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Thông báo lỗi validation (null nếu không có lỗi)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private CancelledOrdersResult() { }

    /// <summary>
    /// Tạo result thành công với dữ liệu
    /// </summary>
    public static CancelledOrdersResult<T> Success(T data)
    {
        return new CancelledOrdersResult<T>
        {
            Data = data,
            IsValid = true,
            ErrorMessage = null
        };
    }

    /// <summary>
    /// Tạo result lỗi validation
    /// </summary>
    public static CancelledOrdersResult<T> ValidationError(string errorMessage)
    {
        return new CancelledOrdersResult<T>
        {
            Data = null,
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}
