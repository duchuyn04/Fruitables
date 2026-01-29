using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;
using ClosedXML.Excel;

namespace Fruitables.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class RevenueController : Controller
    {
        private readonly IRevenueStatisticsService _revenueService;
        private readonly ICancelledOrdersStatisticsService _cancelledOrdersService;

        public RevenueController(
            IRevenueStatisticsService revenueService,
            ICancelledOrdersStatisticsService cancelledOrdersService)
        {
            _revenueService = revenueService;
            _cancelledOrdersService = cancelledOrdersService;
        }

        /// <summary>
        /// Xuất báo cáo doanh thu ra file Excel
        /// GET: /Admin/Revenue/ExportReport
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportReport(DateRangePreset? preset = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime filterStartDate, filterEndDate;
            var selectedPreset = preset ?? DateRangePreset.AllTime;

            if (selectedPreset == DateRangePreset.Custom && startDate.HasValue && endDate.HasValue)
            {
                filterStartDate = startDate.Value;
                filterEndDate = endDate.Value.AddDays(1).AddTicks(-1);
            }
            else if (selectedPreset == DateRangePreset.AllTime)
            {
                var firstOrderDate = await GetFirstOrderDateAsync();
                (filterStartDate, filterEndDate) = selectedPreset.ToDateRange(firstOrderDate);
            }
            else
            {
                (filterStartDate, filterEndDate) = selectedPreset.ToDateRange();
            }

            var overview = await _revenueService.GetRevenueOverviewAsync();
            var categoryRevenue = await _revenueService.GetRevenueByCategoryAsync(filterStartDate, filterEndDate);
            var topProducts = await _revenueService.GetTopProductsAsync(50, filterStartDate, filterEndDate);
            var trend = await _revenueService.GetRevenueTrendAsync(TrendPeriod.Daily, filterStartDate, filterEndDate);

            using var workbook = new XLWorkbook();
            
            // Sheet 1: Tổng quan
            var wsOverview = workbook.Worksheets.Add("Tổng quan");
            CreateOverviewSheet(wsOverview, overview, filterStartDate, filterEndDate);
            
            // Sheet 2: Danh mục
            var wsCategory = workbook.Worksheets.Add("Theo danh mục");
            CreateCategorySheet(wsCategory, categoryRevenue);
            
            // Sheet 3: Sản phẩm bán chạy
            var wsProducts = workbook.Worksheets.Add("Sản phẩm bán chạy");
            CreateProductsSheet(wsProducts, topProducts);
            
            // Sheet 4: Xu hướng
            var wsTrend = workbook.Worksheets.Add("Xu hướng");
            CreateTrendSheet(wsTrend, trend);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"BaoCaoDoanhThu_{filterStartDate:yyyyMMdd}_{filterEndDate:yyyyMMdd}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private void CreateOverviewSheet(IXLWorksheet ws, RevenueOverviewViewModel overview, DateTime startDate, DateTime endDate)
        {
            // Tiêu đề
            ws.Cell("A1").Value = "BÁO CÁO DOANH THU - FRUITABLES";
            ws.Range("A1:D1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(18)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#81c408"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(1).Height = 30;

            // Thông tin thời gian
            ws.Cell("A3").Value = "Từ ngày:";
            ws.Cell("B3").Value = startDate.ToString("dd/MM/yyyy");
            ws.Cell("A4").Value = "Đến ngày:";
            ws.Cell("B4").Value = endDate.ToString("dd/MM/yyyy");
            ws.Cell("A5").Value = "Ngày xuất:";
            ws.Cell("B5").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Range("A3:A5").Style.Font.SetBold(true);

            // Tổng quan
            ws.Cell("A7").Value = "CHỈ SỐ TỔNG QUAN";
            ws.Range("A7:D7").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f8f9fa"));

            ws.Cell("A8").Value = "Chỉ số";
            ws.Cell("B8").Value = "Giá trị";
            ws.Range("A8:B8").Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#e9ecef"));

            ws.Cell("A9").Value = "Tổng doanh thu";
            ws.Cell("B9").Value = overview.TotalRevenue;
            ws.Cell("B9").Style.NumberFormat.Format = "#,##0 \"VND\"";

            ws.Cell("A10").Value = "Tổng đơn hàng";
            ws.Cell("B10").Value = overview.TotalOrders;

            ws.Cell("A11").Value = "Giá trị TB/đơn";
            ws.Cell("B11").Value = overview.AverageOrderValue;
            ws.Cell("B11").Style.NumberFormat.Format = "#,##0 \"VND\"";

            ws.Cell("A12").Value = "Đơn hàng hôm nay";
            ws.Cell("B12").Value = overview.TodayOrders;

            // Định dạng
            ws.Columns().AdjustToContents();
            ws.Column("A").Width = 20;
            ws.Column("B").Width = 25;
        }

        private void CreateCategorySheet(IXLWorksheet ws, RevenueByCategoryViewModel categoryRevenue)
        {
            ws.Cell("A1").Value = "DOANH THU THEO DANH MỤC";
            ws.Range("A1:E1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Header
            ws.Cell("A3").Value = "STT";
            ws.Cell("B3").Value = "Danh mục";
            ws.Cell("C3").Value = "Số đơn";
            ws.Cell("D3").Value = "Doanh thu (VND)";
            ws.Cell("E3").Value = "Tỷ lệ (%)";
            ws.Range("A3:E3").Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e9ecef"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            var row = 4;
            var stt = 1;
            foreach (var cat in categoryRevenue.Categories)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = cat.CategoryName;
                ws.Cell(row, 3).Value = cat.OrderCount;
                ws.Cell(row, 4).Value = cat.Revenue;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = cat.Percentage;
                ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";
                ws.Range(row, 1, row, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            // Tổng cộng
            ws.Cell(row, 1).Value = "";
            ws.Cell(row, 2).Value = "TỔNG CỘNG";
            ws.Cell(row, 3).Value = categoryRevenue.Categories.Sum(c => c.OrderCount);
            ws.Cell(row, 4).Value = categoryRevenue.TotalRevenue;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Value = 100;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";
            ws.Range(row, 1, row, 5).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#d4edda"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Columns().AdjustToContents();
        }

        private void CreateProductsSheet(IXLWorksheet ws, TopProductsViewModel topProducts)
        {
            ws.Cell("A1").Value = "SẢN PHẨM BÁN CHẠY";
            ws.Range("A1:E1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#ffc107"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Header
            ws.Cell("A3").Value = "Hạng";
            ws.Cell("B3").Value = "Sản phẩm";
            ws.Cell("C3").Value = "Danh mục";
            ws.Cell("D3").Value = "SL bán";
            ws.Cell("E3").Value = "Doanh thu (VND)";
            ws.Range("A3:E3").Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e9ecef"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            var row = 4;
            var rank = 1;
            foreach (var product in topProducts.Products)
            {
                ws.Cell(row, 1).Value = rank;
                ws.Cell(row, 2).Value = product.ProductName;
                ws.Cell(row, 3).Value = product.CategoryName;
                ws.Cell(row, 4).Value = product.QuantitySold;
                ws.Cell(row, 5).Value = product.Revenue;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                
                // Highlight top 3
                if (rank <= 3)
                {
                    var color = rank == 1 ? XLColor.FromHtml("#fff3cd") : 
                               rank == 2 ? XLColor.FromHtml("#e2e3e5") : 
                               XLColor.FromHtml("#f8d7da");
                    ws.Range(row, 1, row, 5).Style.Fill.SetBackgroundColor(color);
                }
                
                ws.Range(row, 1, row, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
                rank++;
            }

            ws.Columns().AdjustToContents();
            ws.Column("B").Width = 30;
        }

        private void CreateTrendSheet(IXLWorksheet ws, RevenueTrendViewModel trend)
        {
            ws.Cell("A1").Value = "XU HƯỚNG DOANH THU THEO NGÀY";
            ws.Range("A1:C1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#198754"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Header
            ws.Cell("A3").Value = "Ngày";
            ws.Cell("B3").Value = "Doanh thu (VND)";
            ws.Cell("C3").Value = "Số đơn";
            ws.Range("A3:C3").Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e9ecef"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            var row = 4;
            for (int i = 0; i < trend.Labels.Count; i++)
            {
                ws.Cell(row, 1).Value = trend.Labels[i];
                ws.Cell(row, 2).Value = trend.RevenueData[i];
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 3).Value = trend.OrdersData[i];
                ws.Range(row, 1, row, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            // Tổng cộng
            ws.Cell(row, 1).Value = "TỔNG CỘNG";
            ws.Cell(row, 2).Value = trend.RevenueData.Sum();
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 3).Value = trend.OrdersData.Sum();
            ws.Range(row, 1, row, 3).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#d4edda"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Columns().AdjustToContents();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(DateRangePreset? preset = null)
        {
            var overview = await _revenueService.GetRevenueOverviewAsync();
            var categoryRevenue = await _revenueService.GetRevenueByCategoryAsync();
            var topProducts = await _revenueService.GetTopProductsAsync(10);
            var trend = await _revenueService.GetRevenueTrendAsync(TrendPeriod.Daily);

            var viewModel = new RevenueIndexViewModel
            {
                Overview = overview,
                CategoryRevenue = categoryRevenue,
                TopProducts = topProducts,
                Trend = trend,
                Filter = new RevenueFilterViewModel
                {
                    Preset = preset ?? DateRangePreset.AllTime
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> FilterByPreset([FromBody] RevenueFilterRequest request)
        {
            DateTime startDate, endDate;
            
            var preset = request.GetPresetEnum();
            
            if (preset == null || preset == DateRangePreset.Custom)
            {
                if (!request.StartDate.HasValue || !request.EndDate.HasValue)
                {
                    return BadRequest(new { error = "Vui lòng chọn ngày bắt đầu và kết thúc." });
                }
                startDate = request.StartDate.Value;
                endDate = request.EndDate.Value.AddDays(1).AddTicks(-1);
            }
            else if (preset == DateRangePreset.AllTime)
            {
                var firstOrderDate = await GetFirstOrderDateAsync();
                (startDate, endDate) = preset.Value.ToDateRange(firstOrderDate);
            }
            else
            {
                (startDate, endDate) = preset.Value.ToDateRange();
            }

            var revenueResult = await _revenueService.GetRevenueByDateRangeAsync(startDate, endDate);
            
            if (!revenueResult.IsValid)
            {
                return BadRequest(new { error = revenueResult.ErrorMessage });
            }

            var categoryRevenue = await _revenueService.GetRevenueByCategoryAsync(startDate, endDate);
            var topProducts = await _revenueService.GetTopProductsAsync(10, startDate, endDate, request.CategoryId);
            var trend = await _revenueService.GetRevenueTrendAsync(TrendPeriod.Daily, startDate, endDate);

            return Json(new
            {
                overview = revenueResult.Data,
                categoryRevenue = categoryRevenue,
                topProducts = topProducts,
                trend = trend
            });
        }

        private async Task<DateTime?> GetFirstOrderDateAsync()
        {
            // Get first order date from service using reflection
            var serviceType = _revenueService.GetType();
            var method = serviceType.GetMethod("GetFirstOrderDateAsync");
            if (method != null)
            {
                var task = method.Invoke(_revenueService, null) as Task<DateTime?>;
                return task != null ? await task : null;
            }
            return null;
        }

        /// <summary>
        /// API lấy xu hướng doanh thu theo period
        /// GET: /Admin/Revenue/RevenueTrend
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RevenueTrend(TrendPeriod period = TrendPeriod.Daily, DateTime? startDate = null, DateTime? endDate = null)
        {
            var trend = await _revenueService.GetRevenueTrendAsync(period, startDate, endDate);
            return Json(trend);
        }

        /// <summary>
        /// Trang tổng quan đơn hủy
        /// GET: /Admin/Revenue/CancelledOrders
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CancelledOrders(DateRangePreset? preset = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime? filterStartDate = null;
            DateTime? filterEndDate = null;
            var selectedPreset = preset ?? DateRangePreset.AllTime;

            if (selectedPreset == DateRangePreset.Custom)
            {
                filterStartDate = startDate;
                filterEndDate = endDate?.AddDays(1).AddTicks(-1);
            }
            else if (selectedPreset != DateRangePreset.AllTime)
            {
                (filterStartDate, filterEndDate) = selectedPreset.ToDateRange();
            }

            var overviewResult = await _cancelledOrdersService.GetOverviewAsync(filterStartDate, filterEndDate);
            
            if (!overviewResult.IsValid)
            {
                TempData["Error"] = overviewResult.ErrorMessage;
                return View(new CancelledOrdersOverviewViewModel
                {
                    Filter = new RevenueFilterViewModel { Preset = selectedPreset }
                });
            }

            var overview = overviewResult.Data!;
            overview.Filter = new RevenueFilterViewModel
            {
                Preset = selectedPreset,
                StartDate = filterStartDate,
                EndDate = filterEndDate
            };

            return View(overview);
        }

        /// <summary>
        /// API lấy xu hướng đơn hủy
        /// GET: /Admin/Revenue/CancelledOrdersTrend
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CancelledOrdersTrend(TrendPeriod period = TrendPeriod.Daily, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await _cancelledOrdersService.GetTrendAsync(period, startDate, endDate);
            
            if (!result.IsValid)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Json(result.Data);
        }

        /// <summary>
        /// API lấy thống kê lý do hủy
        /// GET: /Admin/Revenue/CancelReasonStats
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CancelReasonStats(DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await _cancelledOrdersService.GetReasonStatisticsAsync(startDate, endDate);
            
            if (!result.IsValid)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Json(result.Data);
        }

        /// <summary>
        /// API lọc đơn hủy theo preset
        /// POST: /Admin/Revenue/FilterCancelledOrders
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> FilterCancelledOrders([FromBody] RevenueFilterRequest request)
        {
            DateTime? startDate = null;
            DateTime? endDate = null;
            
            var preset = request.GetPresetEnum();
            
            if (preset == null || preset == DateRangePreset.Custom)
            {
                if (!request.StartDate.HasValue || !request.EndDate.HasValue)
                {
                    return BadRequest(new { error = "Vui lòng chọn ngày bắt đầu và kết thúc." });
                }
                startDate = request.StartDate.Value;
                endDate = request.EndDate.Value.AddDays(1).AddTicks(-1);
            }
            else if (preset != DateRangePreset.AllTime)
            {
                (startDate, endDate) = preset.Value.ToDateRange();
            }

            var overviewResult = await _cancelledOrdersService.GetOverviewAsync(startDate, endDate);
            
            if (!overviewResult.IsValid)
            {
                return BadRequest(new { error = overviewResult.ErrorMessage });
            }

            var trendResult = await _cancelledOrdersService.GetTrendAsync(TrendPeriod.Daily, startDate, endDate);
            var reasonResult = await _cancelledOrdersService.GetReasonStatisticsAsync(startDate, endDate);

            return Json(new
            {
                overview = overviewResult.Data,
                trend = trendResult.Data,
                reasons = reasonResult.Data
            });
        }
    }

    public class RevenueFilterRequest
    {
        public string? Preset { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CategoryId { get; set; }
        
        public DateRangePreset? GetPresetEnum()
        {
            if (string.IsNullOrEmpty(Preset))
                return null;
            
            if (Enum.TryParse<DateRangePreset>(Preset, true, out var result))
                return result;
            
            return null;
        }
    }

    public class RevenueIndexViewModel
    {
        public RevenueOverviewViewModel Overview { get; set; } = new();
        public RevenueByCategoryViewModel CategoryRevenue { get; set; } = new();
        public TopProductsViewModel TopProducts { get; set; } = new();
        public RevenueTrendViewModel Trend { get; set; } = new();
        public RevenueFilterViewModel Filter { get; set; } = new();
    }
}
