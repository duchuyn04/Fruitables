// Review Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeReviewActions();
    restoreHelpfulState();

    // Wire nút xác nhận xóa trong modal
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function() {
            if (typeof deleteConfirmCallback === 'function') {
                deleteConfirmCallback();
                deleteConfirmCallback = null;
            }
        });
    }
});

// Khôi phục trạng thái nút Hữu ích đã bấm từ localStorage
function restoreHelpfulState() {
    document.querySelectorAll('.helpful-btn').forEach(btn => {
        const reviewId = btn.getAttribute('data-review-id');
        if (localStorage.getItem(`helpful_voted_${reviewId}`)) {
            btn.disabled = true;
            btn.classList.remove('btn-outline-secondary');
            btn.classList.add('btn-secondary');
            btn.title = 'Bạn đã đánh dấu hữu ích';
        }
    });
}

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
        const token = getAntiForgeryToken();
        
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);
        
        const response = await fetch(`/Review/${reviewId}/helpful`, {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            // Lưu trạng thái đã vote vào localStorage
            localStorage.setItem(`helpful_voted_${reviewId}`, '1');

            // Update helpful count
            const currentCount = parseInt(btn.textContent.match(/\d+/)[0]);
            btn.innerHTML = `<i class="fa fa-thumbs-up"></i> Hữu ích (${currentCount + 1})`;
            btn.classList.remove('btn-outline-secondary');
            btn.classList.add('btn-secondary');
            btn.title = 'Bạn đã đánh dấu hữu ích';
            
            showToast('success', result.message || 'Cảm ơn phản hồi của bạn!');
        } else {
            // Nếu server báo đã vote rồi thì lưu localStorage luôn
            if (result.message && result.message.includes('hữu ích')) {
                localStorage.setItem(`helpful_voted_${reviewId}`, '1');
            }
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

// Biến lưu callback xác nhận xóa
let deleteConfirmCallback = null;

// Delete review — dùng modal Bootstrap thay cho confirm()
function handleDeleteClick(e) {
    const btn = e.currentTarget;
    const reviewId = btn.getAttribute('data-review-id');
    
    if (!reviewId) return;

    // Lưu callback để thực hiện sau khi user xác nhận
    deleteConfirmCallback = () => executeDelete(btn, reviewId);

    // Hiển thị modal xác nhận
    const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
    modal.show();
}

async function executeDelete(btn, reviewId) {
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
                    
                    const remaining = document.querySelectorAll('.review-item').length;

                    // Update count title
                    const countTitle = document.getElementById('reviewsCountTitle');
                    if (countTitle) countTitle.textContent = `Tất cả đánh giá (${remaining})`;

                    // Show empty state if no reviews left (no reload needed)
                    if (remaining === 0) {
                        const reviewsList = document.getElementById('reviewsList');
                        if (reviewsList) {
                            reviewsList.innerHTML = `
                                <div class="text-center py-5">
                                    <i class="fa fa-comments fa-3x text-muted mb-3"></i>
                                    <p class="text-muted">Chưa có đánh giá nào. Hãy là người đầu tiên đánh giá sản phẩm này!</p>
                                </div>`;
                        }
                        // Also hide the sort header
                        const header = document.getElementById('reviewsHeader');
                        if (header) header.style.display = 'none';
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
