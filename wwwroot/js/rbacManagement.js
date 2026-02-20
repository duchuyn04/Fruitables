/**
 * RBAC Management JavaScript
 * Handles role and permission management functionality
 * Requirements: 10.9
 */

// ==================== UTILITY FUNCTIONS ====================

/**
 * Get CSRF token for AJAX requests
 */
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

/**
 * Show toast notification
 * @param {string} message - Message to display
 * @param {string} type - Type of toast ('success' or 'error')
 */
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

/**
 * Create toast container if it doesn't exist
 */
function createToastContainer() {
    const container = document.createElement('div');
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '1100';
    document.body.appendChild(container);
    return container;
}

// ==================== CONFIRMATION DIALOGS ====================

/**
 * Show confirmation dialog before delete operations
 * @param {number} id - ID of the item to delete
 * @param {string} name - Name of the item to delete
 * @param {string} type - Type of item ('role' or 'permission')
 */
function confirmDelete(id, name, type = 'role') {
    const modal = document.getElementById('deleteModal');
    if (!modal) return;
    
    // Update modal content based on type
    const nameElement = document.getElementById(`delete${type === 'role' ? 'Role' : 'Permission'}Name`);
    if (nameElement) {
        nameElement.textContent = name;
    }
    
    // Update form action
    const form = document.getElementById('deleteForm');
    if (form) {
        const baseUrl = type === 'role' ? '/Admin/Role/Delete' : '/Admin/Permission/Delete';
        form.action = `${baseUrl}/${id}`;
    }
    
    // Show modal
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

// ==================== CHECKBOX GROUP SELECTION ====================

/**
 * Select all permissions
 */
function selectAllPermissions() {
    document.querySelectorAll('.permission-checkbox').forEach(cb => {
        cb.checked = true;
    });
    updateSelectedCount();
}

/**
 * Deselect all permissions
 */
function deselectAllPermissions() {
    document.querySelectorAll('.permission-checkbox').forEach(cb => {
        cb.checked = false;
    });
    updateSelectedCount();
}

/**
 * Select all permissions in a specific module
 * @param {string} module - Module name
 */
function selectModulePermissions(module) {
    document.querySelectorAll(`.permission-checkbox[data-module="${module}"]`).forEach(cb => {
        cb.checked = true;
    });
    updateSelectedCount();
}

/**
 * Deselect all permissions in a specific module
 * @param {string} module - Module name
 */
function deselectModulePermissions(module) {
    document.querySelectorAll(`.permission-checkbox[data-module="${module}"]`).forEach(cb => {
        cb.checked = false;
    });
    updateSelectedCount();
}

/**
 * Update the count of selected permissions
 */
function updateSelectedCount() {
    const selectedCountElement = document.getElementById('selectedCount');
    if (selectedCountElement) {
        const count = document.querySelectorAll('.permission-checkbox:checked').length;
        selectedCountElement.textContent = count;
    }
}

/**
 * Reset form to initial state
 */
function resetForm() {
    const form = document.getElementById('assignPermissionsForm');
    if (!form) return;
    
    // Get initial state from data attribute or recreate from checked checkboxes
    const initialState = new Set();
    document.querySelectorAll('.permission-checkbox').forEach(cb => {
        if (cb.hasAttribute('data-initial-checked')) {
            initialState.add(cb.value);
        }
    });
    
    // Reset checkboxes to initial state
    document.querySelectorAll('.permission-checkbox').forEach(cb => {
        cb.checked = initialState.has(cb.value);
    });
    
    updateSelectedCount();
}

// ==================== AJAX OPERATIONS ====================

/**
 * Toggle role active status
 * @param {number} roleId - Role ID
 * @param {boolean} isActive - New active status
 */
async function toggleRoleActive(roleId, isActive) {
    try {
        const response = await fetch('/Admin/Role/ToggleActive', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams({
                'id': roleId,
                'isActive': isActive,
                '__RequestVerificationToken': getAntiForgeryToken()
            })
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || 'Cập nhật trạng thái vai trò thành công');
        } else {
            showToast(result.error || 'Đã xảy ra lỗi khi cập nhật trạng thái vai trò', 'error');
            // Revert checkbox
            const checkbox = document.getElementById(`roleActive_${roleId}`);
            if (checkbox) {
                checkbox.checked = !isActive;
            }
        }
    } catch (error) {
        console.error('Toggle role active error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
        // Revert checkbox
        const checkbox = document.getElementById(`roleActive_${roleId}`);
        if (checkbox) {
            checkbox.checked = !isActive;
        }
    }
}

/**
 * Assign permissions to role via AJAX
 * @param {number} roleId - Role ID
 * @param {Array<number>} permissionIds - Array of permission IDs
 */
async function assignPermissionsToRole(roleId, permissionIds) {
    try {
        const response = await fetch(`/Admin/Role/AssignPermissions/${roleId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ permissionIds })
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || 'Gán quyền hạn thành công');
            // Optionally reload page or update UI
            setTimeout(() => window.location.reload(), 1500);
        } else {
            showToast(result.error || 'Đã xảy ra lỗi khi gán quyền hạn', 'error');
        }
    } catch (error) {
        console.error('Assign permissions error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
    }
}

/**
 * Revoke permission from role via AJAX
 * @param {number} roleId - Role ID
 * @param {number} permissionId - Permission ID
 */
async function revokePermissionFromRole(roleId, permissionId) {
    try {
        const response = await fetch(`/Admin/Role/RevokePermission/${roleId}/${permissionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || 'Thu hồi quyền hạn thành công');
            // Optionally reload page or update UI
            setTimeout(() => window.location.reload(), 1500);
        } else {
            showToast(result.error || 'Đã xảy ra lỗi khi thu hồi quyền hạn', 'error');
        }
    } catch (error) {
        console.error('Revoke permission error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
    }
}

/**
 * Assign role to user via AJAX
 * @param {number} userId - User ID
 * @param {number} roleId - Role ID
 */
async function assignRoleToUser(userId, roleId) {
    try {
        const response = await fetch(`/Admin/User/AssignRole`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ userId, roleId })
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || 'Gán vai trò thành công');
            // Optionally reload page or update UI
            setTimeout(() => window.location.reload(), 1500);
        } else {
            showToast(result.error || 'Đã xảy ra lỗi khi gán vai trò', 'error');
        }
    } catch (error) {
        console.error('Assign role error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
    }
}

/**
 * Revoke role from user via AJAX
 * @param {number} userId - User ID
 * @param {number} roleId - Role ID
 */
async function revokeRoleFromUser(userId, roleId) {
    try {
        const response = await fetch(`/Admin/User/RevokeRole`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ userId, roleId })
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || 'Thu hồi vai trò thành công');
            // Optionally reload page or update UI
            setTimeout(() => window.location.reload(), 1500);
        } else {
            showToast(result.error || 'Đã xảy ra lỗi khi thu hồi vai trò', 'error');
        }
    } catch (error) {
        console.error('Revoke role error:', error);
        showToast('Đã xảy ra lỗi khi kết nối đến server', 'error');
    }
}

// ==================== INITIALIZATION ====================

/**
 * Initialize RBAC management functionality
 */
document.addEventListener('DOMContentLoaded', function() {
    // Initialize toasts
    const toastElList = [].slice.call(document.querySelectorAll('.toast'));
    toastElList.map(function(toastEl) {
        return new bootstrap.Toast(toastEl);
    });
    
    // Store initial state of checkboxes for reset functionality
    document.querySelectorAll('.permission-checkbox:checked').forEach(cb => {
        cb.setAttribute('data-initial-checked', 'true');
    });
    
    // Add change listener to update count
    document.querySelectorAll('.permission-checkbox').forEach(cb => {
        cb.addEventListener('change', updateSelectedCount);
    });
    
    // Initialize selected count
    updateSelectedCount();
    
    // Form submission with loading state
    const assignForm = document.getElementById('assignPermissionsForm');
    if (assignForm) {
        assignForm.addEventListener('submit', function(e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Đang lưu...';
            }
        });
    }
});
