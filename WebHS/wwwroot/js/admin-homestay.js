// Enhanced Admin Homestay Management JavaScript

$(document).ready(function() {
    // Initialize enhanced features
    initializeEnhancedFeatures();
    
    // Initialize keyboard shortcuts
    initializeKeyboardShortcuts();
    
    // Initialize drag and drop for bulk actions
    initializeBulkActions();
    
    // Initialize real-time updates
    initializeRealTimeUpdates();
});

function initializeEnhancedFeatures() {
    // Add smooth scrolling to page
    $('html').css('scroll-behavior', 'smooth');
    
    // Add intersection observer for animations
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        });
        
        $('.stat-card, .main-card').each(function() {
            this.style.opacity = '0';
            this.style.transform = 'translateY(30px)';
            this.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            observer.observe(this);
        });
    }
    
    // Add hover sound effects (optional)
    $('.action-btn').on('mouseenter', function() {
        $(this).addClass('hover-effect');
    });
    
    $('.action-btn').on('mouseleave', function() {
        $(this).removeClass('hover-effect');
    });
    
    // Enhanced image preview
    $('.homestay-image').on('click', function() {
        const src = $(this).attr('src');
        if (src) {
            showImagePreview(src);
        }
    });
    
    // Add loading states to action buttons
    $('.action-btn').on('click', function() {
        const $btn = $(this);
        const originalContent = $btn.html();
        
        $btn.html('<i class="fas fa-spinner fa-spin"></i>');
        $btn.prop('disabled', true);
        
        // Restore after 2 seconds (or when operation completes)
        setTimeout(() => {
            $btn.html(originalContent);
            $btn.prop('disabled', false);
        }, 2000);
    });
}

function initializeKeyboardShortcuts() {
    $(document).on('keydown', function(event) {
        // Ctrl/Cmd + K for search focus
        if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
            event.preventDefault();
            $('#searchInput').focus().select();
        }
        
        // Ctrl/Cmd + N for new homestay
        if ((event.ctrlKey || event.metaKey) && event.key === 'n') {
            event.preventDefault();
            window.location.href = '@Url.Action("CreateHomestay")';
        }
        
        // Escape to clear search
        if (event.key === 'Escape') {
            $('#searchInput').val('').blur();
        }
    });
}

function initializeBulkActions() {
    // Add checkbox column for bulk selection
    if (!$('.table thead th:first-child input[type="checkbox"]').length) {
        $('.table thead tr').prepend('<th><input type="checkbox" id="selectAll" title="Chọn tất cả"></th>');
        $('.table tbody tr').each(function() {
            const homestayId = $(this).find('td:first').text();
            $(this).prepend(`<td><input type="checkbox" class="row-checkbox" value="${homestayId}"></td>`);
        });
    }
    
    // Handle select all
    $('#selectAll').on('change', function() {
        $('.row-checkbox').prop('checked', this.checked);
        updateBulkActionBar();
    });
    
    // Handle individual checkboxes
    $(document).on('change', '.row-checkbox', function() {
        updateBulkActionBar();
        updateSelectAllState();
    });
    
    // Add bulk action bar
    if (!$('#bulkActionBar').length) {
        const bulkActionBar = `
            <div id="bulkActionBar" class="bulk-action-bar" style="display: none;">
                <div class="d-flex justify-content-between align-items-center">
                    <span class="selected-count">0 homestay được chọn</span>
                    <div class="bulk-actions">
                        <button class="btn btn-success btn-sm" onclick="bulkApprove()">
                            <i class="fas fa-check"></i> Duyệt hàng loạt
                        </button>
                        <button class="btn btn-warning btn-sm" onclick="bulkDeactivate()">
                            <i class="fas fa-ban"></i> Vô hiệu hóa
                        </button>
                        <button class="btn btn-info btn-sm" onclick="exportSelected()">
                            <i class="fas fa-download"></i> Xuất Excel
                        </button>
                    </div>
                </div>
            </div>
        `;
        $('.main-card').prepend(bulkActionBar);
    }
}

function updateBulkActionBar() {
    const selectedCount = $('.row-checkbox:checked').length;
    if (selectedCount > 0) {
        $('#bulkActionBar').slideDown();
        $('.selected-count').text(`${selectedCount} homestay được chọn`);
    } else {
        $('#bulkActionBar').slideUp();
    }
}

function updateSelectAllState() {
    const totalCheckboxes = $('.row-checkbox').length;
    const checkedCheckboxes = $('.row-checkbox:checked').length;
    
    $('#selectAll').prop('indeterminate', checkedCheckboxes > 0 && checkedCheckboxes < totalCheckboxes);
    $('#selectAll').prop('checked', checkedCheckboxes === totalCheckboxes);
}

function initializeRealTimeUpdates() {
    // Auto-refresh data every 30 seconds
    setInterval(() => {
        updateStatistics();
    }, 30000);
    
    // Check for new homestays pending approval
    setInterval(() => {
        checkPendingHomestays();
    }, 60000);
}

function updateStatistics() {
    $.get('@Url.Action("GetHomestayStatistics")', function(data) {
        if (data.success) {
            animateNumberChange('.stat-card:eq(0) .stat-number', data.total);
            animateNumberChange('.stat-card:eq(1) .stat-number', data.approved);
            animateNumberChange('.stat-card:eq(2) .stat-number', data.pending);
            animateNumberChange('.stat-card:eq(3) .stat-number', data.inactive);
        }
    });
}

function animateNumberChange(selector, newValue) {
    const $element = $(selector);
    const currentValue = parseInt($element.text()) || 0;
    
    if (currentValue !== newValue) {
        $({ value: currentValue }).animate({ value: newValue }, {
            duration: 1000,
            step: function() {
                $element.text(Math.round(this.value));
            },
            complete: function() {
                $element.text(newValue);
                // Add highlight effect
                $element.parent().addClass('highlight-update');
                setTimeout(() => {
                    $element.parent().removeClass('highlight-update');
                }, 2000);
            }
        });
    }
}

function checkPendingHomestays() {
    $.get('@Url.Action("GetPendingCount")', function(data) {
        if (data.success && data.count > 0) {
            showPendingNotification(data.count);
        }
    });
}

function showPendingNotification(count) {
    if (!$('#pendingNotification').length) {
        const notification = `
            <div id="pendingNotification" class="pending-notification">
                <i class="fas fa-bell"></i>
                <span>Có ${count} homestay chờ duyệt mới</span>
                <button onclick="$(this).parent().fadeOut()">×</button>
            </div>
        `;
        $('body').append(notification);
        
        setTimeout(() => {
            $('#pendingNotification').fadeOut();
        }, 5000);
    }
}

function showImagePreview(src) {
    const modal = `
        <div class="modal fade" id="imagePreviewModal" tabindex="-1">
            <div class="modal-dialog modal-lg modal-dialog-centered">
                <div class="modal-content bg-transparent border-0">
                    <div class="modal-body p-0">
                        <button type="button" class="btn-close btn-close-white position-absolute top-0 end-0 m-3" 
                                data-bs-dismiss="modal" style="z-index: 1060;"></button>
                        <img src="${src}" class="img-fluid rounded" style="width: 100%;">
                    </div>
                </div>
            </div>
        </div>
    `;
    
    if ($('#imagePreviewModal').length) {
        $('#imagePreviewModal').remove();
    }
    
    $('body').append(modal);
    $('#imagePreviewModal').modal('show');
    
    $('#imagePreviewModal').on('hidden.bs.modal', function() {
        $(this).remove();
    });
}

// Bulk actions
function bulkApprove() {
    const selectedIds = $('.row-checkbox:checked').map(function() {
        return this.value;
    }).get();
    
    if (selectedIds.length === 0) return;
    
    if (confirm(`Bạn có chắc chắn muốn duyệt ${selectedIds.length} homestay được chọn?`)) {
        showLoading();
        $.post('@Url.Action("BulkApprove")', { ids: selectedIds }, function(data) {
            hideLoading();
            if (data.success) {
                showSuccessNotification(`✅ Đã duyệt ${data.count} homestay thành công!`);
                setTimeout(() => location.reload(), 1500);
            } else {
                showErrorNotification('❌ Có lỗi xảy ra: ' + data.message);
            }
        });
    }
}

function bulkDeactivate() {
    const selectedIds = $('.row-checkbox:checked').map(function() {
        return this.value;
    }).get();
    
    if (selectedIds.length === 0) return;
    
    if (confirm(`Bạn có chắc chắn muốn vô hiệu hóa ${selectedIds.length} homestay được chọn?`)) {
        showLoading();
        $.post('@Url.Action("BulkDeactivate")', { ids: selectedIds }, function(data) {
            hideLoading();
            if (data.success) {
                showSuccessNotification(`✅ Đã vô hiệu hóa ${data.count} homestay thành công!`);
                setTimeout(() => location.reload(), 1500);
            } else {
                showErrorNotification('❌ Có lỗi xảy ra: ' + data.message);
            }
        });
    }
}

function exportSelected() {
    const selectedIds = $('.row-checkbox:checked').map(function() {
        return this.value;
    }).get();
    
    if (selectedIds.length === 0) {
        showErrorNotification('⚠️ Vui lòng chọn ít nhất một homestay');
        return;
    }
    
    const form = $('<form>', {
        method: 'POST',
        action: '@Url.Action("ExportSelectedHomestays")'
    });
    
    selectedIds.forEach(id => {
        form.append($('<input>', {
            type: 'hidden',
            name: 'ids',
            value: id
        }));
    });
    
    $('body').append(form);
    form.submit();
    form.remove();
    
    showSuccessNotification('📄 Đang tạo file Excel...');
}

// Additional CSS for new features
const additionalStyles = `
<style>
.bulk-action-bar {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 1rem;
    border-radius: 10px;
    margin-bottom: 1rem;
    box-shadow: 0 5px 20px rgba(0,0,0,0.1);
}

.highlight-update {
    animation: highlight 2s ease-in-out;
}

@keyframes highlight {
    0%, 100% { background: transparent; }
    50% { background: rgba(102, 126, 234, 0.2); }
}

.pending-notification {
    position: fixed;
    top: 100px;
    left: 20px;
    background: linear-gradient(135deg, #ffc107 0%, #fd7e14 100%);
    color: white;
    padding: 1rem 1.5rem;
    border-radius: 10px;
    box-shadow: 0 10px 30px rgba(0,0,0,0.2);
    z-index: 10000;
    animation: slideInLeft 0.5s ease-out;
}

@keyframes slideInLeft {
    from { transform: translateX(-100%); }
    to { transform: translateX(0); }
}

.hover-effect {
    transform: scale(1.1) !important;
    z-index: 10;
}

.row-checkbox {
    transform: scale(1.2);
}

input[type="checkbox"]:checked {
    accent-color: #667eea;
}
</style>
`;

$('head').append(additionalStyles);
