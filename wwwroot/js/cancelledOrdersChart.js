/**
 * Cancelled Orders Chart and Filter JavaScript
 * Handles chart rendering and filter functionality for cancelled orders statistics
 */

// Chart instances
let cancelledTrendChart = null;
let cancelReasonChart = null;

// Current filter state
let currentFilterPreset = 'AllTime';
let currentStartDate = null;
let currentEndDate = null;
let currentPeriod = 'Daily';

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initFilterHandlers();
    initPeriodHandlers();
    loadInitialData();
});

/**
 * Load initial data from API
 */
function loadInitialData() {
    // Load trend data
    loadTrendData(currentPeriod);
    // Load reason statistics
    loadReasonStats();
}

/**
 * Initialize filter button handlers
 */
function initFilterHandlers() {
    // Preset button handlers
    document.querySelectorAll('.preset-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.preset-btn').forEach(b => {
                b.classList.remove('btn-primary');
                b.classList.add('btn-outline-primary');
            });
            this.classList.remove('btn-outline-primary');
            this.classList.add('btn-primary');
            
            const preset = this.dataset.preset;
            document.getElementById('filterPreset').value = preset;
            currentFilterPreset = preset;
            applyFilter(preset);
        });
    });

    // Custom date apply button
    const applyBtn = document.getElementById('applyCustomDateBtn');
    if (applyBtn) {
        applyBtn.addEventListener('click', function() {
            const startDate = document.getElementById('filterStartDate').value;
            const endDate = document.getElementById('filterEndDate').value;
            
            if (!startDate || !endDate) {
                alert('Vui lòng chọn ngày bắt đầu và kết thúc.');
                return;
            }
            
            currentStartDate = startDate;
            currentEndDate = endDate;
            applyFilter('Custom', startDate, endDate);
        });
    }
}

/**
 * Initialize period toggle button handlers
 */
function initPeriodHandlers() {
    document.querySelectorAll('[data-period]').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('[data-period]').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            currentPeriod = this.dataset.period;
            loadTrendData(currentPeriod);
        });
    });
}


/**
 * Apply filter and reload all data
 */
function applyFilter(preset, startDate = null, endDate = null) {
    currentFilterPreset = preset;
    
    const requestData = {
        preset: preset,
        startDate: startDate,
        endDate: endDate
    };

    fetch('/Admin/Revenue/FilterCancelledOrders', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify(requestData)
    })
    .then(response => {
        if (!response.ok) {
            return response.json().then(err => { throw new Error(err.error); });
        }
        return response.json();
    })
    .then(data => {
        updateDashboard(data);
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Lỗi: ' + error.message);
    });
}

/**
 * Load trend data from API
 */
function loadTrendData(period) {
    let url = `/Admin/Revenue/CancelledOrdersTrend?period=${period}`;
    
    if (currentFilterPreset === 'Custom' && currentStartDate && currentEndDate) {
        url += `&startDate=${currentStartDate}&endDate=${currentEndDate}`;
    }

    fetch(url)
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => { throw new Error(err.error); });
            }
            return response.json();
        })
        .then(data => {
            initCancelledTrendChart(data);
        })
        .catch(error => {
            console.error('Error loading trend data:', error);
        });
}

/**
 * Load reason statistics from API
 */
function loadReasonStats() {
    let url = '/Admin/Revenue/CancelReasonStats';
    
    if (currentFilterPreset === 'Custom' && currentStartDate && currentEndDate) {
        url += `?startDate=${currentStartDate}&endDate=${currentEndDate}`;
    }

    fetch(url)
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => { throw new Error(err.error); });
            }
            return response.json();
        })
        .then(data => {
            initCancelReasonChart(data);
            updateReasonTable(data);
        })
        .catch(error => {
            console.error('Error loading reason stats:', error);
        });
}

/**
 * Update dashboard with new data
 */
function updateDashboard(data) {
    // Update overview cards
    if (data.overview) {
        document.getElementById('totalCancelledOrders').textContent = data.overview.totalCancelledOrders;
        document.getElementById('cancellationRate').textContent = data.overview.cancellationRate.toFixed(1) + '%';
        document.getElementById('totalCancelledValue').textContent = data.overview.totalCancelledValue.toLocaleString('vi-VN');
        document.getElementById('totalOrders').textContent = data.overview.totalOrders;
    }

    // Update trend chart
    if (data.trend) {
        initCancelledTrendChart(data.trend);
    }

    // Update reason chart and table
    if (data.reasons) {
        initCancelReasonChart(data.reasons);
        updateReasonTable(data.reasons);
    }
}


/**
 * Initialize cancelled orders trend chart
 */
function initCancelledTrendChart(data) {
    const ctx = document.getElementById('cancelledTrendChart');
    if (!ctx) return;

    if (cancelledTrendChart) {
        cancelledTrendChart.destroy();
    }

    cancelledTrendChart = new Chart(ctx.getContext('2d'), {
        type: 'line',
        data: {
            labels: data.labels || [],
            datasets: [{
                label: 'Số đơn hủy',
                data: data.cancelledData || [],
                borderColor: '#dc3545',
                backgroundColor: 'rgba(220, 53, 69, 0.1)',
                fill: true,
                tension: 0.4,
                yAxisID: 'y'
            }, {
                label: 'Tỷ lệ hủy (%)',
                data: data.cancellationRateData || [],
                borderColor: '#ffc107',
                backgroundColor: 'rgba(255, 193, 7, 0.1)',
                fill: false,
                tension: 0.4,
                yAxisID: 'y1'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1,
                        callback: function(value) {
                            if (Number.isInteger(value)) {
                                return value;
                            }
                        }
                    },
                    title: {
                        display: true,
                        text: 'Số đơn hủy'
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    beginAtZero: true,
                    max: 100,
                    grid: {
                        drawOnChartArea: false
                    },
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    },
                    title: {
                        display: true,
                        text: 'Tỷ lệ hủy'
                    }
                }
            },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            if (context.datasetIndex === 0) {
                                return 'Đơn hủy: ' + context.raw;
                            }
                            return 'Tỷ lệ hủy: ' + context.raw.toFixed(1) + '%';
                        }
                    }
                },
                legend: {
                    position: 'top'
                }
            }
        }
    });
}

/**
 * Initialize cancel reason pie chart
 */
function initCancelReasonChart(data) {
    const ctx = document.getElementById('cancelReasonChart');
    if (!ctx) return;

    if (cancelReasonChart) {
        cancelReasonChart.destroy();
    }

    const reasons = data.reasons || [];
    
    if (reasons.length === 0) {
        // Show empty state
        cancelReasonChart = new Chart(ctx.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Không có dữ liệu'],
                datasets: [{
                    data: [1],
                    backgroundColor: ['#e9ecef'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        enabled: false
                    }
                }
            }
        });
        return;
    }

    const colors = [
        '#dc3545', '#fd7e14', '#ffc107', '#20c997', '#0dcaf0',
        '#6f42c1', '#d63384', '#6c757d', '#198754', '#0d6efd'
    ];

    cancelReasonChart = new Chart(ctx.getContext('2d'), {
        type: 'doughnut',
        data: {
            labels: reasons.map(r => r.reason),
            datasets: [{
                data: reasons.map(r => r.count),
                backgroundColor: colors.slice(0, reasons.length),
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        usePointStyle: true,
                        font: {
                            size: 11
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((context.raw / total) * 100).toFixed(1);
                            return context.label + ': ' + context.raw + ' đơn (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });
}


/**
 * Update cancel reason table
 */
function updateReasonTable(data) {
    const tbody = document.querySelector('#cancelReasonTable tbody');
    if (!tbody) return;

    const reasons = data.reasons || [];

    if (reasons.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="text-center text-muted py-4">
                    <i class="fas fa-info-circle me-2"></i>Không có đơn hủy trong khoảng thời gian này
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = reasons.map((reason, index) => `
        <tr>
            <td>${index + 1}</td>
            <td>
                <span class="badge bg-light text-dark">${escapeHtml(reason.reason)}</span>
            </td>
            <td class="text-end">${reason.count}</td>
            <td class="text-end">
                <div class="d-flex align-items-center justify-content-end">
                    <div class="progress flex-grow-1 me-2" style="height: 6px; max-width: 80px;">
                        <div class="progress-bar bg-danger" style="width: ${reason.percentage}%"></div>
                    </div>
                    <span>${reason.percentage.toFixed(1)}%</span>
                </div>
            </td>
        </tr>
    `).join('');
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
