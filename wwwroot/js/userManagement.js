/**
 * User Management JavaScript
 * Handles lock/unlock account functionality with modals and validation
 * Requirements: 1.1, 4.1, 5.1, 8.2
 */

// Global state for lock/unlock operations
let lockState = {
    customerId: null,
    customerName: '',
    isVip: false,
    hasPendingOrders: false,
    pendingOrderCount: 0,
    pendingOrderNumbers: [],
    isPermanentLock: false
};

let unlockState = {
    customerId: null,
    customerName: '',
    isPermanentLock: false,
    hasAbnormalHistory: false
};

// CSRF Token helper
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// Toast notification helper
function showToast(message, type = 'success') {
    const toastContainer = document.querySelector('.toast-container') || createToastContainer();
    
    const toastHtml = `
        <div class="toast show" role="alert" data-bs-autohide="true" data-bs-delay="5000">
            <div class="toast-header bg-${type === 'success' ? 'success' : 'danger'} text-white">
                <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
                <strong class="me-auto">${type === 'success' ? 'Thành công' : 'Lỗi'}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${message}</div>
        </div>
    `;
    
    const toastElement = document.createElement('div');
    toastElement.innerHTML = toastHtml;
    const toast = toastElement.firstElementChild;
    toastContainer.appendChild(toast);
    
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    // Remove toast after it hides
    toast.addEventListener('hidden.bs.toast', () => toast.remove());
}

function createToastContainer() {
    const container = document.createElement('div');
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '1100';
    document.body.appendChild(container);
    return container;
}

// ==================== LOCK ACCOUNT FUNCTIONS ====================

/**
 * Show lock account modal
 * Requirements: 4.1, 4.4, 7.3
 */
function showLockModal(customerId, customerName, isVip = false) {
    lockState.customerId = customerId;
    lockState.customerName = customerName;
    lockState.isVip = isVip;
    lockState.hasPendingOrders = false;
    lockState.pendingOrderCount = 0;
    lockState.pendingOrderNumbers = [];
    
    // Reset form
    document.getElementById('lockAccountForm').reset();
    document.getElementById('lockCustomerId').value = customerId;
    document.getElementById('lockCustomerName').textContent = customerName;
    document.getElementById('lockConfirmWithPendingOrders').value = 'false';
    
    // Reset character count
    document.getElementById('lockReasonCount').textContent = '0';
    
    // Show/hide VIP warning
    const vipWarning = document.getElementById('vipWarning');
    if (isVip) {
        vipWarning.classList.remove('d-none');
    } else {
        vipWarning.classList.add('d-none');
    }
    
    // Hide pending orders warning initially
    document.getElementById('pendingOrdersWarning').classList.add('d-none');
    
    // Reset lock type to temporary
    document.getElementById('lockTypeTemporary').checked = true;
    document.getElementById('lockDurationContainer').classList.remove('d-none');
    document.getElementById('permanentLockWarning').classList.add('d-none');
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('lockAccountModal'));
    modal.show();
}

/**
 * Handle lock type change
 * Requirements: 4.3
 */
document.addEventListener('DOMContentLoaded', function() {
    // Lock type radio buttons
    const lockTypeRadios = document.querySelectorAll('input[name="LockType"]');
    lockTypeRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            const isTemporary = this.value === '0';
            const durationContainer = document.getElementById('lockDurationContainer');
            const permanentWarning = document.getElementById('permanentLockWarning');
            
            if (isTemporary) {
                durationContainer.classList.remove('d-none');
                permanentWarning.classList.add('d-none');
                document.getElementById('lockDurationDays').required = true;
            } else {
                durationContainer.classList.add('d-none');
                permanentWarning.classList.remove('d-none');
                document.getElementById('lockDurationDays').required = false;
            }
            
            lockState.isPermanentLock = !isTemporary;
        });
    });
    
    // Lock reason character count
    const lockReasonInput = document.getElementById('lockReason');
    if (lockReasonInput) {
        lockReasonInput.addEventListener('input', function() {
            document.getElementById('lockReasonCount').textContent = this.value.length;
        });
    }
    
    // Unlock reason character count
    const unlockReasonInput = document.getElementById('unlockReason');
    if (unlockReasonInput) {
        unlockReasonInput.addEventListener('input', function() {
            document.getElementById('unlockReasonCount').textContent = this.value.length;
        });
    }
});

/**
 * Validate lock form
 * Requirements: 4.1, 4.2, 4.3
 */
function validateLockForm() {
    const form = document.getElementById('lockAccountForm');
    const violationType = document.getElementById('violationType').value;
    const reason = document.getElementById('lockReason').value;
    const lockType = document.querySelector('input[name="LockType"]:checked').value;
    const durationDays = document.getElementById('lockDurationDays').value;
    
    let isValid = true;
    
    // Validate violation type
    if (!violationType) {
        document.getElementById('violationType').classList.add('is-invalid');
        isValid = false;
    } else {
        document.getElementById('violationType').classList.remove('is-invalid');
    }
    
    // Validate reason (min 20 characters)
    if (reason.length < 20) {
        document.getElementById('lockReason').classList.add('is-invalid');
        isValid = false;
    } else {
        document.getElementById('lockReason').classList.remove('is-invalid');
    }
    
    // Validate duration for temporary lock
    if (lockType === '0') {
        const days = parseInt(durationDays);
        if (isNaN(days) || days < 1 || days > 365) {
            document.getElementById('lockDurationDays').classList.add('is-invalid');
            isValid = false;
        } else {
            document.getElementById('lockDurationDays').classList.remove('is-invalid');
        }
    }
    
    return isValid;
}

/**
 * Submit lock account request
 * Requirements: 4.1, 4.4, 4.5
 */
function submitLockAccount() {
    if (!validateLockForm()) {
        return;
    }
    
    // Prepare confirmation modal
    const violationType = document.getElementById('violationType').value;
    const reason = document.getElementById('lockReason').value;
    const lockType = document.querySelector('input[name="LockType"]:checked').value;
    const durationDays = document.getElementById('lockDurationDays').value;
    
    document.getElementById('confirmCustomerName').textContent = lockState.customerName;
    document.getElementById('confirmViolationType').textContent = violationType;
    document.getElementById('confirmLockType').textContent = lockType === '0' 
        ? `Tạm thời (${durationDays} ngày)` 
        : 'Vĩnh viễn';
    document.getElementById('confirmReason').textContent = reason.length > 100 
        ? reason.substring(0, 100) + '...' 
        : reason;
    
    // Show/hide VIP double confirmation
    const vipDoubleConfirm = document.getElementById('vipDoubleConfirm');
    if (lockState.isVip) {
        vipDoubleConfirm.classList.remove('d-none');
        document.getElementById('vipConfirmCheckbox').checked = false;
    } else {
        vipDoubleConfirm.classList.add('d-none');
    }
    
    // Hide pending orders confirmation initially (will be shown if needed after API call)
    document.getElementById('pendingOrdersConfirm').classList.add('d-none');
    
    // Hide lock modal and show confirmation modal
    bootstrap.Modal.getInstance(document.getElementById('lockAccountModal')).hide();
    const confirmModal = new bootstrap.Modal(document.getElementById('lockConfirmModal'));
    confirmModal.show();
}

/**
 * Cancel lock confirmation and return to lock modal
 */
function cancelLockConfirmation() {
    bootstrap.Modal.getInstance(document.getElementById('lockConfirmModal')).hide();
    const lockModal = new bootstrap.Modal(document.getElementById('lockAccountModal'));
    lockModal.show();
}

/**
 * Confirm and execute lock account
 * Requirements: 4.5, 4.6, 7.3, 8.2
 */
async function confirmLockAccount() {
    // Check VIP confirmation if needed
    if (lockState.isVip && !document.getElementById('vipConfirmCheckbox').checked) {
        showToast('Vui lòng xác nhận khóa tài khoản khách hàng VIP', 'error');
        return;
    }
    
    // Check pending orders confirmation if needed
    const pendingOrdersConfirm = document.getElementById('pendingOrdersConfirm');
    if (!pendingOrdersConfirm.classList.contains('d-none') && 
        !document.getElementById('pendingOrdersConfirmCheckbox').checked) {
        showToast('Vui lòng xác nhận khóa dù có đơn hàng đang xử lý', 'error');
        return;
    }
    
    const lockType = document.querySelector('input[name="LockType"]:checked').value;
    const durationDays = document.getElementById('lockDurationDays').value;
    
    const request = {
        customerId: lockState.customerId,
        violationType: document.getElementById('violationType').value,
        reason: document.getElementById('lockReason').value,
        lockType: parseInt(lockType),
        lockDurationDays: lockType === '0' ? parseInt(durationDays) : null,
        confirmLockWithPendingOrders: !pendingOrdersConfirm.classList.contains('d-none')
    };
    
    // Disable button and show loading
    const confirmBtn = document.getElementById('confirmLockBtn');
    const originalText = confirmBtn.innerHTML;
    confirmBtn.disabled = true;
    confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';
    
    try {
        const response = await fetch('/Admin/User/Lock', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(request)
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            // Check if requires confirmation for pending orders
            if (result.data && result.data.requiresConfirmation && result.data.hasPendingOrders && !request.confirmLockWithPendingOrders) {
                // Show pending orders warning
                lockState.hasPendingOrders = true;
                lockState.pendingOrderCount = result.data.pendingOrderCount;
                lockState.pendingOrderNumbers = result.data.pendingOrderNumbers || [];
                
                document.getElementById('pendingOrdersConfirm').classList.remove('d-none');
                document.getElementById('pendingOrdersConfirmCheckbox').checked = false;
                
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = originalText;
                return;
            }
            
            // Success - close modal and refresh page
            bootstrap.Modal.getInstance(document.getElementById('lockConfirmModal')).hide();
            showToast('Khóa tài khoản thành công');
            
            // Refresh page after short delay
            setTimeout(() => window.location.reload(), 1500);
        } else {
            // Handle error
            const errorMessage = result.error || 'Đã xảy ra lỗi khi khóa tài khoản';
            
            // Handle concurrency error
            if (result.errorCode === 'ConcurrencyError') {
                showToast(errorMessage, 'error');
                setTimeout(() => window.location.reload(), 2000);
            } else {
                showToast(errorMessage, 'error');
            }
            
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        }
    } catch (error) {
        console.error('Lock account error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
        confirmBtn.disabled = false;
        confirmBtn.innerHTML = originalText;
    }
}

// ==================== UNLOCK ACCOUNT FUNCTIONS ====================

/**
 * Show unlock account modal
 * Requirements: 5.1, 5.2, 5.5
 */
function showUnlockModal(customerId, customerName, isPermanentLock = false, hasAbnormalHistory = false) {
    unlockState.customerId = customerId;
    unlockState.customerName = customerName;
    unlockState.isPermanentLock = isPermanentLock;
    unlockState.hasAbnormalHistory = hasAbnormalHistory;
    
    // Reset form
    document.getElementById('unlockAccountForm').reset();
    document.getElementById('unlockCustomerId').value = customerId;
    document.getElementById('unlockCustomerName').textContent = customerName;
    
    // Reset character count
    document.getElementById('unlockReasonCount').textContent = '0';
    
    // Show/hide permanent lock warning
    const permanentWarning = document.getElementById('permanentUnlockWarning');
    if (isPermanentLock) {
        permanentWarning.classList.remove('d-none');
    } else {
        permanentWarning.classList.add('d-none');
    }
    
    // Show/hide abnormal history warning
    const abnormalWarning = document.getElementById('abnormalHistoryWarning');
    if (hasAbnormalHistory) {
        abnormalWarning.classList.remove('d-none');
    } else {
        abnormalWarning.classList.add('d-none');
    }
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('unlockAccountModal'));
    modal.show();
}

/**
 * Validate unlock form
 * Requirements: 5.1
 */
function validateUnlockForm() {
    const reason = document.getElementById('unlockReason').value;
    
    // Validate reason (min 10 characters)
    if (reason.length < 10) {
        document.getElementById('unlockReason').classList.add('is-invalid');
        return false;
    } else {
        document.getElementById('unlockReason').classList.remove('is-invalid');
        return true;
    }
}

/**
 * Submit unlock account request
 * Requirements: 5.1, 5.2
 */
function submitUnlockAccount() {
    if (!validateUnlockForm()) {
        return;
    }
    
    const reason = document.getElementById('unlockReason').value;
    
    // Prepare confirmation modal
    document.getElementById('confirmUnlockCustomerName').textContent = unlockState.customerName;
    document.getElementById('confirmUnlockReason').textContent = reason.length > 100 
        ? reason.substring(0, 100) + '...' 
        : reason;
    
    // Show/hide permanent lock double confirmation
    const permanentDoubleConfirm = document.getElementById('permanentUnlockDoubleConfirm');
    if (unlockState.isPermanentLock) {
        permanentDoubleConfirm.classList.remove('d-none');
        document.getElementById('permanentUnlockConfirmCheckbox').checked = false;
    } else {
        permanentDoubleConfirm.classList.add('d-none');
    }
    
    // Show/hide abnormal history confirmation
    const abnormalConfirm = document.getElementById('abnormalHistoryConfirm');
    if (unlockState.hasAbnormalHistory) {
        abnormalConfirm.classList.remove('d-none');
        document.getElementById('abnormalHistoryConfirmCheckbox').checked = false;
    } else {
        abnormalConfirm.classList.add('d-none');
    }
    
    // Hide unlock modal and show confirmation modal
    bootstrap.Modal.getInstance(document.getElementById('unlockAccountModal')).hide();
    const confirmModal = new bootstrap.Modal(document.getElementById('unlockConfirmModal'));
    confirmModal.show();
}

/**
 * Cancel unlock confirmation and return to unlock modal
 */
function cancelUnlockConfirmation() {
    bootstrap.Modal.getInstance(document.getElementById('unlockConfirmModal')).hide();
    const unlockModal = new bootstrap.Modal(document.getElementById('unlockAccountModal'));
    unlockModal.show();
}

/**
 * Confirm and execute unlock account
 * Requirements: 5.2, 5.3, 8.2
 */
async function confirmUnlockAccount() {
    // Check permanent lock confirmation if needed
    const permanentConfirm = document.getElementById('permanentUnlockDoubleConfirm');
    if (!permanentConfirm.classList.contains('d-none') && 
        !document.getElementById('permanentUnlockConfirmCheckbox').checked) {
        showToast('Vui lòng xác nhận mở khóa tài khoản bị khóa vĩnh viễn', 'error');
        return;
    }
    
    // Check abnormal history confirmation if needed
    const abnormalConfirm = document.getElementById('abnormalHistoryConfirm');
    if (!abnormalConfirm.classList.contains('d-none') && 
        !document.getElementById('abnormalHistoryConfirmCheckbox').checked) {
        showToast('Vui lòng xác nhận mở khóa dù tài khoản có lịch sử bất thường', 'error');
        return;
    }
    
    const request = {
        customerId: unlockState.customerId,
        reason: document.getElementById('unlockReason').value
    };
    
    // Disable button and show loading
    const confirmBtn = document.getElementById('confirmUnlockBtn');
    const originalText = confirmBtn.innerHTML;
    confirmBtn.disabled = true;
    confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';
    
    try {
        const response = await fetch('/Admin/User/Unlock', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(request)
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            // Success - close modal and refresh page
            bootstrap.Modal.getInstance(document.getElementById('unlockConfirmModal')).hide();
            showToast('Mở khóa tài khoản thành công');
            
            // Refresh page after short delay
            setTimeout(() => window.location.reload(), 1500);
        } else {
            // Handle error
            const errorMessage = result.error || 'Đã xảy ra lỗi khi mở khóa tài khoản';
            
            // Handle concurrency error
            if (result.errorCode === 'ConcurrencyError') {
                showToast(errorMessage, 'error');
                setTimeout(() => window.location.reload(), 2000);
            } else {
                showToast(errorMessage, 'error');
            }
            
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        }
    } catch (error) {
        console.error('Unlock account error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
        confirmBtn.disabled = false;
        confirmBtn.innerHTML = originalText;
    }
}
