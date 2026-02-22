using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Fruitables.Attributes;
using Fruitables.Services.Interfaces;
using Fruitables.ViewModels;

namespace Fruitables.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo review mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("reviews.create")]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            var userId = GetCurrentUserId();
            var result = await _reviewService.CreateReviewAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage, errorCode = result.ErrorCode });
            }

            return Ok(new { success = true, message = "Đánh giá của bạn đã được gửi thành công", data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo đánh giá" });
        }
    }

    /// <summary>
    /// Cập nhật review
    /// </summary>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    [RequirePermission("reviews.edit_own")]
    public async Task<IActionResult> Edit(int id, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            var userId = GetCurrentUserId();
            var result = await _reviewService.UpdateReviewAsync(id, dto, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage, errorCode = result.ErrorCode });
            }

            return Ok(new { success = true, message = "Đánh giá đã được cập nhật", data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi cập nhật đánh giá" });
        }
    }

    /// <summary>
    /// Xóa review
    /// </summary>
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    [RequirePermission("reviews.delete_own")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _reviewService.DeleteReviewAsync(id, userId);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Không thể xóa đánh giá" });
            }

            return Ok(new { success = true, message = "Đánh giá đã được xóa" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xóa đánh giá" });
        }
    }

    /// <summary>
    /// Báo cáo review vi phạm
    /// </summary>
    [HttpPost("{id}/report")]
    [ValidateAntiForgeryToken]
    [RequirePermission("reviews.view")]
    public async Task<IActionResult> Report(int id, [FromBody] ReportReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            var userId = GetCurrentUserId();
            var success = await _reviewService.ReportReviewAsync(id, dto, userId);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Không thể báo cáo đánh giá này" });
            }

            return Ok(new { success = true, message = "Báo cáo của bạn đã được gửi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting review {ReviewId}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi báo cáo" });
        }
    }

    /// <summary>
    /// Đánh dấu review hữu ích
    /// </summary>
    [HttpPost("{id}/helpful")]
    [ValidateAntiForgeryToken]
    [RequirePermission("reviews.view")]
    public async Task<IActionResult> MarkHelpful(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _reviewService.MarkReviewHelpfulAsync(id, userId);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Không thể đánh dấu hữu ích" });
            }

            return Ok(new { success = true, message = "Cảm ơn phản hồi của bạn" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking review {ReviewId} as helpful", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra" });
        }
    }

    /// <summary>
    /// Kiểm tra user có thể review sản phẩm không
    /// </summary>
    [HttpGet("can-review/{productId}")]
    [RequirePermission("reviews.view")]
    public async Task<IActionResult> CanReview(int productId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var canReview = await _reviewService.CanUserReviewProductAsync(userId, productId);

            return Ok(new { canReview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can review product {ProductId}", productId);
            return StatusCode(500, new { canReview = false });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
