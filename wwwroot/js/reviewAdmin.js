// Review Admin Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize modals
    const hideModal = new bootstrap.Modal(document.getElementById('hideModal'));
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

    // Hide review buttons
    document.querySelectorAll('.btn-hide').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            document.getElementById('hideReviewId').value = reviewId;
            document.getElementById('hideReason').value = '';
            hideModal.show();
        });
    });

    // Show review buttons
    document.querySelectorAll('.btn-show').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            if (confirm('Bạn có chắc muốn hiện đánh giá này?')) {
                showReview(reviewId);
            }
        });
    });

    // Delete review buttons
    document.querySelectorAll('.btn-delete').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            document.getElementById('deleteReviewId').value = reviewId;
            document.getElementById('deleteReason').value = '';
            deleteModal.show();
        });
    });

    // Confirm hide
    document.getElementById('confirmHide')?.addEventListener('click', function() {
        const reviewId = document.getElementById('hideReviewId').value;
        const reason = document.getElementById('hideReason').value.trim();

        if (!reason) {
            showToast('Vui lòng nhập lý do ẩn', 'warning');
            return;
        }

        hideReview(reviewId, reason);
    });

    // Confirm delete
    document.getElementById('confirmDelete')?.addEventListener('click', function() {
        const reviewId = document.getElementById('deleteReviewId').value;
        const reason = document.getElementById('deleteReason').value.trim();

        if (!reason) {
            showToast('Vui lòng nhập lý do xóa', 'warning');
            return;
        }

        deleteReview(reviewId, reason);
    });

    // Resolve report buttons
    document.querySelectorAll('.btn-resolve').forEach(btn => {
        btn.addEventListener('click', function() {
            const reportId = this.dataset.id;
            if (confirm('Xử lý báo cáo này? Đánh giá sẽ bị ẩn.')) {
                handleReport(reportId, 'Resolve');
            }
        });
    });

    // Dismiss report buttons
    document.querySelectorAll('.btn-dismiss').forEach(btn => {
        btn.addEventListener('click', function() {
            const reportId = this.dataset.id;
            if (confirm('Bỏ qua báo cáo này?')) {
                handleReport(reportId, 'Dismiss');
            }
        });
    });

    // Select all checkbox
    document.getElementById('selectAll')?.addEventListener('change', function() {
        document.querySelectorAll('.review-checkbox').forEach(cb => {
            cb.checked = this.checked;
        });
    });
});

// Hide review
async function hideReview(reviewId, reason) {
    try {
        const formData = new FormData();
        formData.append('id', reviewId);
        formData.append('reason', reason);

        const response = await fetch('/Admin/ReviewAdmin/Hide', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            bootstrap.Modal.getInstance(document.getElementById('hideModal')).hide();
            setTimeout(() => location.reload(), 1000);
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error hiding review:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// Show review
async function showReview(reviewId) {
    try {
        const formData = new FormData();
        formData.append('id', reviewId);

        const response = await fetch('/Admin/ReviewAdmin/Show', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            setTimeout(() => location.reload(), 1000);
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error showing review:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// Delete review
async function deleteReview(reviewId, reason) {
    try {
        const formData = new FormData();
        formData.append('id', reviewId);
        formData.append('reason', reason);

        const response = await fetch('/Admin/ReviewAdmin/Delete', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
            setTimeout(() => location.reload(), 1000);
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error deleting review:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// Handle report
async function handleReport(reportId, action) {
    try {
        const formData = new FormData();
        formData.append('id', reportId);
        formData.append('action', action);

        const response = await fetch('/Admin/ReviewAdmin/HandleReport', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            setTimeout(() => location.reload(), 1000);
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error handling report:', error);
        showToast('Có lỗi xảy ra', 'error');
    }
}

// Show toast notification
function showToast(message, type = 'info') {
    // Create toast element
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'warning'} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    // Create container if not exists
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        document.body.appendChild(container);
    }

    // Add toast
    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastElement = container.lastElementChild;
    const toast = new bootstrap.Toast(toastElement);
    toast.show();

    // Remove after hidden
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}
