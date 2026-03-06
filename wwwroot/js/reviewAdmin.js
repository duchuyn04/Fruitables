// Review Admin Management JavaScript

let hideModal, deleteModal;
let filterTimeout = null;

document.addEventListener('DOMContentLoaded', function() {
    // Initialize modals
    hideModal = new bootstrap.Modal(document.getElementById('hideModal'));
    deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

    bindReviewEvents();
    bindFilterEvents();

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
});

function bindReviewEvents() {
    const container = document.getElementById('reviewTableContainer');
    if (!container) return;

    // Hide review buttons
    container.querySelectorAll('.btn-hide').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            document.getElementById('hideReviewId').value = reviewId;
            document.getElementById('hideReason').value = '';
            hideModal.show();
        });
    });

    // Show review buttons
    container.querySelectorAll('.btn-show').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            if (confirm('Bạn có chắc muốn hiện đánh giá này?')) {
                showReview(reviewId);
            }
        });
    });

    // Delete review buttons
    container.querySelectorAll('.btn-delete').forEach(btn => {
        btn.addEventListener('click', function() {
            const reviewId = this.dataset.id;
            document.getElementById('deleteReviewId').value = reviewId;
            document.getElementById('deleteReason').value = '';
            deleteModal.show();
        });
    });

    // Resolve report buttons
    container.querySelectorAll('.btn-resolve').forEach(btn => {
        btn.addEventListener('click', function() {
            const reportId = this.dataset.id;
            if (confirm('Xử lý báo cáo này? Đánh giá sẽ bị ẩn.')) {
                handleReport(reportId, 'Resolve');
            }
        });
    });

    // Dismiss report buttons
    container.querySelectorAll('.btn-dismiss').forEach(btn => {
        btn.addEventListener('click', function() {
            const reportId = this.dataset.id;
            if (confirm('Bỏ qua báo cáo này?')) {
                handleReport(reportId, 'Dismiss');
            }
        });
    });

    // Select all checkbox
    const selectAll = container.querySelector('#selectAll');
    if (selectAll) {
        selectAll.addEventListener('change', function() {
            container.querySelectorAll('.review-checkbox').forEach(cb => {
                cb.checked = this.checked;
            });
        });
    }

    // Pagination links
    container.querySelectorAll('.ajax-page').forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            loadReviewsAjax(this.getAttribute('href'));
        });
    });
}

function bindFilterEvents() {
    const filterForm = document.getElementById('filterForm');
    if (!filterForm) return;

    // Prevent default form submit and use AJAX
    filterForm.addEventListener('submit', function(e) {
        e.preventDefault();
        triggerFilter();
    });

    // Listeners for select dropdowns
    filterForm.querySelectorAll('select').forEach(select => {
        select.addEventListener('change', function() {
            triggerFilter();
        });
    });

    // Listener for search text input (with debounce)
    const searchInput = filterForm.querySelector('input[name="SearchTerm"]');
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            clearTimeout(filterTimeout);
            filterTimeout = setTimeout(triggerFilter, 500); // 500ms debounce
        });
    }

    // Reset button should also use ajax
    const resetBtn = filterForm.querySelector('a[href*="Index"]');
    if (resetBtn) {
        resetBtn.addEventListener('click', function(e) {
            e.preventDefault();
            // clear form
            filterForm.reset();
            // trigger
            triggerFilter();
        });
    }
}

function triggerFilter() {
    const filterForm = document.getElementById('filterForm');
    const formData = new FormData(filterForm);
    const params = new URLSearchParams(formData);
    
    // Remove empty parameters to clean URL
    const keysForDel = [];
    params.forEach((value, key) => {
        if (value.trim() === '') {
            keysForDel.push(key);
        }
    });
    keysForDel.forEach(k => params.delete(k));

    const url = filterForm.getAttribute('action') + '?' + params.toString();
    loadReviewsAjax(url);
}

async function loadReviewsAjax(url) {
    const container = document.getElementById('reviewTableContainer');
    if (!container) return;

    try {
        // Opacity effect to show loading state
        container.style.opacity = '0.5';

        const response = await fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const html = await response.text();
            container.innerHTML = html;
            
            // push state to url so user can copy url or refresh
            window.history.pushState(null, '', url);
            
            // Re-bind events to new DOM elements
            bindReviewEvents();
        } else {
            showToast('Lỗi khi tải dữ liệu', 'error');
        }
    } catch (error) {
        console.error('Error fetching reviews:', error);
        showToast('Có lỗi xảy ra', 'error');
    } finally {
        container.style.opacity = '1';
    }
}

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
