// Review Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeReviewActions();
});

function initializeReviewActions() {
    // Helpful button handlers
    document.querySelectorAll('.helpful-btn').forEach(btn => {
        btn.addEventListener('click', handleHelpfulClick);
    });

    // Edit button handlers
    document.querySelectorAll('.edit-review-btn').forEach(btn => {
        btn.addEventListener('click', handleEditClick);
    });

    // Delete button handlers
    document.querySelectorAll('.delete-review-btn').forEach(btn => {
        btn.addEventListener('click', handleDeleteClick);
    });
}

// Mark review as helpful
async function handleHelpfulClick(e) {
    const btn = e.currentTarget;
    const reviewId = btn.getAttribute('data-review-id');
    
    if (!reviewId) return;

    // Disable button to prevent double-click
    btn.disabled = true;

    try {
        const response = await fetch(`/Review/${reviewId}/helpful`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        const result = await response.json();

        if (result.success) {
            // Update helpful count
            const currentCount = parseInt(btn.textContent.match(/\d+/)[0]);
            btn.innerHTML = `<i class="fa fa-thumbs-up"></i> Hữu ích (${currentCount + 1})`;
            
            // Show success message
            showToast('success', result.message || 'Cảm ơn phản hồi của bạn!');
        } else {
            showToast('error', result.message || 'Không thể đánh dấu hữu ích');
            btn.disabled = false;
        }
    } catch (error) {
        console.error('Error marking review as helpful:', error);
        showToast('error', 'Có lỗi xảy ra. Vui lòng thử lại.');
        btn.disabled = false;
    }
}

// Edit review
function handleEditClick(e) {
    const btn = e.currentTarget;
    const reviewId = btn.getAttribute('data-review-id');
    const rating = btn.getAttribute('data-rating');
    const comment = btn.getAttribute('data-comment');
    
    if (typeof openEditReviewModal === 'function') {
        openEditReviewModal(reviewId, rating, comment);
    }
}

// Delete review
async function handleDeleteClick(e) {
    const btn = e.currentTarget;
    const reviewId = btn.getAttribute('data-review-id');
    
    if (!reviewId) return;

    // Confirm deletion
    if (!confirm('Bạn có chắc chắn muốn xóa đánh giá này?')) {
        return;
    }

    // Disable button
    btn.disabled = true;
    const originalHtml = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

    try {
        const response = await fetch(`/Review/${reviewId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        const result = await response.json();

        if (result.success) {
            showToast('success', result.message || 'Đánh giá đã được xóa');
            
            // Remove review item from DOM
            const reviewItem = btn.closest('.review-item');
            if (reviewItem) {
                reviewItem.style.transition = 'opacity 0.3s ease';
                reviewItem.style.opacity = '0';
                setTimeout(() => {
                    reviewItem.remove();
                    
                    // Reload page if no reviews left
                    if (document.querySelectorAll('.review-item').length === 0) {
                        window.location.reload();
                    }
                }, 300);
            }
        } else {
            showToast('error', result.message || 'Không thể xóa đánh giá');
            btn.disabled = false;
            btn.innerHTML = originalHtml;
        }
    } catch (error) {
        console.error('Error deleting review:', error);
        showToast('error', 'Có lỗi xảy ra. Vui lòng thử lại.');
        btn.disabled = false;
        btn.innerHTML = originalHtml;
    }
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// Show toast notification
function showToast(type, message) {
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed top-0 end-0 m-3`;
    toast.style.zIndex = '9999';
    toast.style.minWidth = '300px';
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;
    
    document.body.appendChild(toast);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        toast.style.transition = 'opacity 0.3s ease';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}
