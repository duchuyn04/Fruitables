/**
 * Revenue Filter JavaScript
 * Handles preset selection and AJAX calls for revenue statistics filtering
 * 
 * Requirements: 2.3, 2.5, 2.10
 */

(function() {
    'use strict';

    // Configuration
    const config = {
        debounceDelay: 300,
        endpoints: {
            index: '/Admin/Revenue',
            chartData: '/Admin/Revenue/GetChartData',
            byCategory: '/Admin/Revenue/ByCategory',
            topProducts: '/Admin/Revenue/TopProducts'
        }
    };

    // State
    let debounceTimer = null;
    let currentPreset = null;
    let isLoading = false;

    /**
     * Initialize the revenue filter functionality
     */
    function init() {
        // Get initial preset from hidden field
        const presetField = document.getElementById('filterPreset');
        if (presetField && presetField.value) {
            currentPreset = presetField.value;
        }

        // Bind preset button click handlers
        bindPresetButtons();

        // Bind custom date range handlers
        bindCustomDateHandlers();

        // Bind apply button handler
        bindApplyButton();
    }

    /**
     * Bind click handlers to all preset buttons
     */
    function bindPresetButtons() {
        const presetButtons = document.querySelectorAll('.preset-btn');
        presetButtons.forEach(button => {
            button.addEventListener('click', function() {
                const preset = this.getAttribute('data-preset');
                selectPreset(preset);
            });
        });
    }

    /**
     * Bind change handlers to custom date inputs
     */
    function bindCustomDateHandlers() {
        const startDateInput = document.getElementById('filterStartDate');
        const endDateInput = document.getElementById('filterEndDate');

        if (startDateInput) {
            startDateInput.addEventListener('change', function() {
                // Clear preset selection when custom dates are entered
                clearPresetSelection();
                updateHiddenPreset('Custom');
            });
        }

        if (endDateInput) {
            endDateInput.addEventListener('change', function() {
                // Clear preset selection when custom dates are entered
                clearPresetSelection();
                updateHiddenPreset('Custom');
            });
        }
    }

    /**
     * Bind click handler to apply button
     */
    function bindApplyButton() {
        const applyBtn = document.getElementById('applyCustomDateBtn');
        if (applyBtn) {
            applyBtn.addEventListener('click', function() {
                applyCustomDateRange();
            });
        }
    }

    /**
     * Select a preset and trigger data reload
     * @param {string} preset - The preset value to select
     */
    function selectPreset(preset) {
        if (isLoading) return;

        // Update UI
        updatePresetButtonUI(preset);
        
        // Clear custom date inputs
        clearCustomDateInputs();
        
        // Update hidden field
        updateHiddenPreset(preset);
        
        // Store current preset
        currentPreset = preset;

        // Load data with the selected preset
        loadRevenueData({ preset: preset });
    }

    /**
     * Update the visual state of preset buttons
     * @param {string} selectedPreset - The currently selected preset
     */
    function updatePresetButtonUI(selectedPreset) {
        const presetButtons = document.querySelectorAll('.preset-btn');
        presetButtons.forEach(button => {
            const buttonPreset = button.getAttribute('data-preset');
            if (buttonPreset === selectedPreset) {
                button.classList.remove('btn-outline-primary');
                button.classList.add('btn-primary');
            } else {
                button.classList.remove('btn-primary');
                button.classList.add('btn-outline-primary');
            }
        });
    }

    /**
     * Clear the preset button selection
     */
    function clearPresetSelection() {
        const presetButtons = document.querySelectorAll('.preset-btn');
        presetButtons.forEach(button => {
            button.classList.remove('btn-primary');
            button.classList.add('btn-outline-primary');
        });
        currentPreset = null;
    }

    /**
     * Clear custom date input fields
     */
    function clearCustomDateInputs() {
        const startDateInput = document.getElementById('filterStartDate');
        const endDateInput = document.getElementById('filterEndDate');
        
        if (startDateInput) startDateInput.value = '';
        if (endDateInput) endDateInput.value = '';
    }

    /**
     * Update the hidden preset field
     * @param {string} preset - The preset value to set
     */
    function updateHiddenPreset(preset) {
        const presetField = document.getElementById('filterPreset');
        if (presetField) {
            presetField.value = preset;
        }
    }

    /**
     * Apply custom date range filter
     */
    function applyCustomDateRange() {
        if (isLoading) return;

        const startDateInput = document.getElementById('filterStartDate');
        const endDateInput = document.getElementById('filterEndDate');

        const startDate = startDateInput ? startDateInput.value : null;
        const endDate = endDateInput ? endDateInput.value : null;

        if (!startDate || !endDate) {
            showToast('Vui lòng chọn cả ngày bắt đầu và ngày kết thúc', 'warning');
            return;
        }

        // Validate date range
        if (new Date(startDate) > new Date(endDate)) {
            showToast('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc', 'error');
            return;
        }

        // Clear preset selection
        clearPresetSelection();
        updateHiddenPreset('Custom');

        // Load data with custom date range
        loadRevenueData({
            startDate: startDate,
            endDate: endDate,
            preset: 'Custom'
        });
    }

    /**
     * Load revenue data via AJAX
     * @param {Object} params - The filter parameters
     */
    function loadRevenueData(params) {
        if (isLoading) return;

        isLoading = true;
        showLoadingIndicator(true);

        // Build request body for POST
        const requestBody = {};
        
        if (params.preset) {
            // Map preset string to enum value
            requestBody.preset = mapPresetToEnum(params.preset);
        }
        
        if (params.startDate) {
            requestBody.startDate = params.startDate;
        }
        
        if (params.endDate) {
            requestBody.endDate = params.endDate;
        }

        // Get category filter if exists
        const categoryField = document.getElementById('filterCategoryId');
        if (categoryField && categoryField.value) {
            requestBody.categoryId = parseInt(categoryField.value);
        }

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        fetch('/Admin/Revenue/FilterByPreset', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token || ''
            },
            body: JSON.stringify(requestBody)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    throw new Error(err.error || 'Network response was not ok');
                });
            }
            return response.json();
        })
        .then(data => {
            updateRevenueContent(data, params.preset || 'Custom');
            updateBrowserUrl(params);
        })
        .catch(error => {
            console.error('Error loading revenue data:', error);
            showToast('Lỗi: ' + error.message, 'error');
        })
        .finally(() => {
            isLoading = false;
            showLoadingIndicator(false);
        });
    }

    /**
     * Map preset string - just return the string as-is since controller expects string
     * @param {string} preset - The preset string
     * @returns {string} The preset string
     */
    function mapPresetToEnum(preset) {
        // Controller expects string, not enum number
        return preset;
    }

    /**
     * Update revenue content with JSON data
     * @param {Object} data - The revenue data from API
     * @param {string} preset - The selected preset
     */
    function updateRevenueContent(data, preset) {
        // Update overview cards with preset for dynamic label
        if (data.overview) {
            updateOverviewCards(data.overview, preset);
        }

        // Update category chart
        if (data.categoryRevenue) {
            updateCategoryChart(data.categoryRevenue);
        }

        // Update top products table
        if (data.topProducts) {
            updateTopProductsTable(data.topProducts);
        }

        // Update trend chart
        if (data.trend) {
            updateTrendChart(data.trend);
        }
    }

    /**
     * Update overview cards with new data
     * @param {Object} overview - The overview data
     * @param {string} preset - The selected preset for dynamic label
     */
    function updateOverviewCards(overview, preset) {
        const formatNumber = (value) => {
            return new Intl.NumberFormat('vi-VN').format(value || 0);
        };

        // Update total revenue (use id selector)
        const totalRevenueEl = document.getElementById('totalRevenue');
        if (totalRevenueEl) {
            totalRevenueEl.textContent = formatNumber(overview.totalRevenue);
        }

        // Update monthly revenue - show filtered revenue
        const monthlyRevenueEl = document.getElementById('monthlyRevenue');
        if (monthlyRevenueEl) {
            monthlyRevenueEl.textContent = formatNumber(overview.totalRevenue);
        }

        // Update label dynamically based on preset
        const monthlyLabelEl = document.getElementById('monthlyRevenueLabel');
        if (monthlyLabelEl) {
            monthlyLabelEl.textContent = getPresetLabel(preset);
        }

        // Hide monthly growth badge when filtered (not applicable for custom periods)
        const monthlyGrowthContainer = document.getElementById('monthlyGrowthContainer');
        if (monthlyGrowthContainer) {
            monthlyGrowthContainer.style.display = preset === 'ThisMonth' ? '' : 'none';
        }

        // Update total orders
        const totalOrdersEl = document.getElementById('totalOrders');
        if (totalOrdersEl) {
            totalOrdersEl.textContent = overview.totalOrders || 0;
        }

        // Update today orders badge - hide when not viewing today/this month
        const todayOrdersEl = document.getElementById('todayOrders');
        if (todayOrdersEl) {
            todayOrdersEl.style.display = (preset === 'Today' || preset === 'ThisMonth') ? '' : 'none';
        }

        // Update AOV
        const aovEl = document.getElementById('avgOrderValue');
        if (aovEl) {
            aovEl.textContent = formatNumber(overview.averageOrderValue);
        }
    }

    /**
     * Get Vietnamese label for preset
     * @param {string} preset - The preset value
     * @returns {string} Vietnamese label
     */
    function getPresetLabel(preset) {
        const labels = {
            'Today': 'Doanh thu hôm nay',
            'Yesterday': 'Doanh thu hôm qua',
            'Last7Days': 'Doanh thu 7 ngày',
            'LastWeek': 'Doanh thu tuần trước',
            'Last30Days': 'Doanh thu 30 ngày',
            'ThisMonth': 'Doanh thu tháng này',
            'LastMonth': 'Doanh thu tháng trước',
            'ThisYear': 'Doanh thu năm nay',
            'AllTime': 'Doanh thu tất cả',
            'Custom': 'Doanh thu (tùy chọn)'
        };
        return labels[preset] || 'Doanh thu (theo bộ lọc)';
    }

    /**
     * Update category chart with new data
     * @param {Object} categoryRevenue - The category revenue data
     */
    function updateCategoryChart(categoryRevenue) {
        if (window.categoryChart && categoryRevenue.categories) {
            const labels = categoryRevenue.categories.map(c => c.categoryName);
            const data = categoryRevenue.categories.map(c => c.revenue);
            
            window.categoryChart.data.labels = labels;
            window.categoryChart.data.datasets[0].data = data;
            window.categoryChart.update();
        }
    }

    /**
     * Update top products table with new data
     * @param {Object} topProducts - The top products data
     */
    function updateTopProductsTable(topProducts) {
        const tableBody = document.querySelector('#topProductsTable tbody');
        if (!tableBody || !topProducts.products) return;

        const formatCurrency = (value) => {
            return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
        };

        tableBody.innerHTML = topProducts.products.map((product, index) => `
            <tr>
                <td>${index + 1}</td>
                <td>${product.productName}</td>
                <td>${product.categoryName}</td>
                <td class="text-end">${product.quantitySold}</td>
                <td class="text-end">${formatCurrency(product.revenue)}</td>
            </tr>
        `).join('');
    }

    /**
     * Update trend chart with new data
     * @param {Object} trend - The trend data
     */
    function updateTrendChart(trend) {
        if (window.trendChart && trend.labels) {
            window.trendChart.data.labels = trend.labels;
            window.trendChart.data.datasets[0].data = trend.revenueData;
            window.trendChart.update();
        }
    }

    /**
     * Get the current page endpoint
     * @returns {string} The current endpoint URL
     */
    function getCurrentEndpoint() {
        // Determine endpoint based on current page
        const path = window.location.pathname.toLowerCase();
        
        if (path.includes('/bycategory')) {
            return config.endpoints.byCategory;
        } else if (path.includes('/topproducts')) {
            return config.endpoints.topProducts;
        }
        
        return config.endpoints.index;
    }

    /**
     * Update the page content with new HTML
     * @param {string} html - The HTML content to insert
     */
    function updateContent(html) {
        const contentContainer = document.getElementById('revenueContent');
        if (contentContainer) {
            contentContainer.innerHTML = html;
        } else {
            // If no specific container, reload the page
            window.location.reload();
        }
    }

    /**
     * Update browser URL without page reload
     * @param {Object} params - The filter parameters
     */
    function updateBrowserUrl(params) {
        const queryParams = new URLSearchParams();
        if (params.preset) queryParams.append('preset', params.preset);
        if (params.startDate) queryParams.append('startDate', params.startDate);
        if (params.endDate) queryParams.append('endDate', params.endDate);
        
        const newUrl = `${window.location.pathname}?${queryParams.toString()}`;
        window.history.replaceState({}, '', newUrl);
    }

    /**
     * Show or hide loading indicator
     * @param {boolean} show - Whether to show the indicator
     */
    function showLoadingIndicator(show) {
        const indicator = document.getElementById('loadingIndicator');
        if (indicator) {
            if (show) {
                indicator.classList.remove('d-none');
            } else {
                indicator.classList.add('d-none');
            }
        }
    }

    /**
     * Show a toast notification
     * @param {string} message - The message to display
     * @param {string} type - The toast type (success, error, warning, info)
     */
    function showToast(message, type = 'info') {
        // Check if Bootstrap toast is available
        if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
            const toastContainer = document.querySelector('.toast-container');
            if (!toastContainer) {
                // Create toast container if it doesn't exist
                const container = document.createElement('div');
                container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
                container.style.zIndex = '1100';
                document.body.appendChild(container);
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

            const container = document.querySelector('.toast-container');
            container.insertAdjacentHTML('beforeend', toastHtml);
            
            const toastEl = container.lastElementChild;
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

    // Expose public API
    window.RevenueFilter = {
        init: init,
        selectPreset: selectPreset,
        applyCustomDateRange: applyCustomDateRange,
        loadRevenueData: loadRevenueData
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
