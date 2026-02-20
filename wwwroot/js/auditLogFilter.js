/**
 * Audit Log Filter JavaScript
 * Handles filtering functionality for RBAC audit log view
 * 
 * Requirements: 10.7
 */

(function() {
    'use strict';

    // Configuration
    const config = {
        debounceDelay: 300,
        endpoint: '/Admin/RbacAudit'
    };

    // State
    let debounceTimer = null;
    let isLoading = false;

    /**
     * Initialize the audit log filter functionality
     */
    function init() {
        // Bind filter change handlers
        bindFilterHandlers();

        // Bind clear filters button
        bindClearButton();

        // Initialize date range picker if available
        initializeDateRangePicker();

        // Initialize toasts
        initializeToasts();

        // Load filters from URL parameters
        loadFiltersFromUrl();
    }

    /**
     * Bind change handlers to all filter inputs
     */
    function bindFilterHandlers() {
        const filterInputs = [
            'filterEntityType',
            'filterEntityId',
            'filterChangedBy',
            'filterStartDate',
            'filterEndDate'
        ];

        filterInputs.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                // Auto-apply filters on change
                element.addEventListener('change', function() {
                    debounce(applyFilters, config.debounceDelay);
                });

                // For text/number inputs, also listen to input event
                if (element.type === 'text' || element.type === 'number') {
                    element.addEventListener('input', function() {
                        debounce(applyFilters, config.debounceDelay);
                    });
                }
            }
        });
    }

    /**
     * Bind click handler to clear filters button
     */
    function bindClearButton() {
        const clearBtn = document.querySelector('button[onclick="clearFilters()"]');
        if (clearBtn) {
            // Remove inline onclick and use proper event listener
            clearBtn.removeAttribute('onclick');
            clearBtn.addEventListener('click', clearFilters);
        }
    }

    /**
     * Initialize date range picker functionality
     */
    function initializeDateRangePicker() {
        const startDateInput = document.getElementById('filterStartDate');
        const endDateInput = document.getElementById('filterEndDate');

        if (startDateInput && endDateInput) {
            // Set max date to today
            const today = new Date().toISOString().split('T')[0];
            startDateInput.setAttribute('max', today);
            endDateInput.setAttribute('max', today);

            // Validate date range
            startDateInput.addEventListener('change', function() {
                if (endDateInput.value && this.value > endDateInput.value) {
                    showToast('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc', 'warning');
                    this.value = '';
                }
            });

            endDateInput.addEventListener('change', function() {
                if (startDateInput.value && this.value < startDateInput.value) {
                    showToast('Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu', 'warning');
                    this.value = '';
                }
            });
        }
    }

    /**
     * Initialize Bootstrap toasts
     */
    function initializeToasts() {
        const toastElList = [].slice.call(document.querySelectorAll('.toast'));
        toastElList.map(function(toastEl) {
            return new bootstrap.Toast(toastEl);
        });
    }

    /**
     * Load filters from URL parameters
     */
    function loadFiltersFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);

        const filterMap = {
            'entityType': 'filterEntityType',
            'entityId': 'filterEntityId',
            'changedBy': 'filterChangedBy',
            'startDate': 'filterStartDate',
            'endDate': 'filterEndDate'
        };

        Object.keys(filterMap).forEach(param => {
            const value = urlParams.get(param);
            if (value) {
                const element = document.getElementById(filterMap[param]);
                if (element) {
                    element.value = value;
                }
            }
        });
    }

    /**
     * Apply filters and reload the page
     */
    function applyFilters() {
        if (isLoading) return;

        const params = new URLSearchParams();

        // Get filter values
        const entityType = document.getElementById('filterEntityType')?.value;
        const entityId = document.getElementById('filterEntityId')?.value;
        const changedBy = document.getElementById('filterChangedBy')?.value;
        const startDate = document.getElementById('filterStartDate')?.value;
        const endDate = document.getElementById('filterEndDate')?.value;

        // Validate date range before applying
        if (startDate && endDate && startDate > endDate) {
            showToast('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc', 'warning');
            return;
        }

        // Build query parameters
        if (entityType) params.append('entityType', entityType);
        if (entityId) params.append('entityId', entityId);
        if (changedBy) params.append('changedBy', changedBy);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);

        // Always start from page 1 when filters change
        params.append('page', '1');

        // Update URL and reload
        const newUrl = `${config.endpoint}?${params.toString()}`;
        window.location.href = newUrl;
    }

    /**
     * Clear all filters
     */
    function clearFilters() {
        // Clear all filter inputs
        const filterInputs = [
            'filterEntityType',
            'filterEntityId',
            'filterChangedBy',
            'filterStartDate',
            'filterEndDate'
        ];

        filterInputs.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.value = '';
            }
        });

        // Reload page without filters
        window.location.href = config.endpoint;
    }

    /**
     * Load a specific page with current filters
     * @param {number} page - The page number to load
     */
    function loadPage(page) {
        if (isLoading) return;

        const params = new URLSearchParams();

        // Get current filter values
        const entityType = document.getElementById('filterEntityType')?.value;
        const entityId = document.getElementById('filterEntityId')?.value;
        const changedBy = document.getElementById('filterChangedBy')?.value;
        const startDate = document.getElementById('filterStartDate')?.value;
        const endDate = document.getElementById('filterEndDate')?.value;

        // Build query parameters
        if (entityType) params.append('entityType', entityType);
        if (entityId) params.append('entityId', entityId);
        if (changedBy) params.append('changedBy', changedBy);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        params.append('page', page);

        // Update URL and reload
        const newUrl = `${config.endpoint}?${params.toString()}`;
        window.location.href = newUrl;
    }

    /**
     * Show details modal
     * @param {number} logId - The log ID
     * @param {string} oldValue - The old value
     * @param {string} newValue - The new value
     */
    function showDetails(logId, oldValue, newValue) {
        const oldValueContent = document.getElementById('oldValueContent');
        const newValueContent = document.getElementById('newValueContent');

        if (oldValueContent) {
            oldValueContent.textContent = oldValue || '(Không có)';
        }

        if (newValueContent) {
            newValueContent.textContent = newValue || '(Không có)';
        }

        const modalEl = document.getElementById('detailsModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    }

    /**
     * Show a toast notification
     * @param {string} message - The message to display
     * @param {string} type - The toast type (success, error, warning, info)
     */
    function showToast(message, type = 'info') {
        if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
            let toastContainer = document.querySelector('.toast-container');
            
            // Create toast container if it doesn't exist
            if (!toastContainer) {
                toastContainer = document.createElement('div');
                toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
                toastContainer.style.zIndex = '1100';
                document.body.appendChild(toastContainer);
            }

            const bgClass = {
                'success': 'bg-success',
                'error': 'bg-danger',
                'warning': 'bg-warning',
                'info': 'bg-info'
            }[type] || 'bg-info';

            const iconClass = {
                'success': 'fa-check-circle',
                'error': 'fa-exclamation-circle',
                'warning': 'fa-exclamation-triangle',
                'info': 'fa-info-circle'
            }[type] || 'fa-info-circle';

            const toastHtml = `
                <div class="toast" role="alert" data-bs-autohide="true" data-bs-delay="5000">
                    <div class="toast-header ${bgClass} text-white">
                        <i class="fas ${iconClass} me-2"></i>
                        <strong class="me-auto">Thông báo</strong>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                    </div>
                    <div class="toast-body">${message}</div>
                </div>
            `;

            toastContainer.insertAdjacentHTML('beforeend', toastHtml);
            
            const toastEl = toastContainer.lastElementChild;
            const toast = new bootstrap.Toast(toastEl);
            toast.show();

            // Remove toast element after it's hidden
            toastEl.addEventListener('hidden.bs.toast', function() {
                toastEl.remove();
            });
        } else {
            // Fallback to alert
            alert(message);
        }
    }

    /**
     * Debounce function to limit rapid calls
     * @param {Function} func - The function to debounce
     * @param {number} delay - The delay in milliseconds
     */
    function debounce(func, delay) {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(func, delay);
    }

    /**
     * Update the total count display
     * @param {number} count - The total count
     */
    function updateTotalCount(count) {
        const totalCountEl = document.getElementById('totalCount');
        if (totalCountEl) {
            totalCountEl.textContent = count;
        }
    }

    // Expose public API
    window.AuditLogFilter = {
        init: init,
        applyFilters: applyFilters,
        clearFilters: clearFilters,
        loadPage: loadPage,
        showDetails: showDetails,
        updateTotalCount: updateTotalCount
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
