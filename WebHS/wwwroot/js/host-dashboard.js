// Enhanced Host Dashboard JavaScript - Fixed Version
$(document).ready(function() {
    // Chỉ gọi initializeCharts nếu có canvas
    if (document.getElementById('revenueChart')) {
        setTimeout(() => {
            initializeCharts();
        }, 100);
    }
    initializeDashboard();
    initializeCalendar();
    initializeQuickActions();
});

function initializeDashboard() {
    // Add loading animation to stat cards
    $('.fade-in').each(function(index) {
        $(this).css('animation-delay', (index * 0.1) + 's');
    });

    // Add hover effects to quick action buttons
    $('.quick-action-btn').hover(
        function() {
            $(this).addClass('shadow-lg');
            $(this).find('i').addClass('text-primary');
        },
        function() {
            $(this).removeClass('shadow-lg');
            $(this).find('i').removeClass('text-primary');
        }
    );

    // Add click animations
    $('.quick-action-btn').click(function() {
        $(this).css('transform', 'scale(0.95)');
        setTimeout(() => {
            $(this).css('transform', '');
        }, 150);
    });

    // Refresh button handler
    $(".btn-refresh").on('click', function() {
        initializeCharts();
    });
}

function initializeQuickActions() {
    // Add loading state to quick action buttons
    $('.quick-action-btn').click(function(e) {
        const $btn = $(this);
        const originalHtml = $btn.html();
        
        // Check if button has href attribute for navigation
        const href = $btn.attr('href');
        if (href && href !== '#') {
            // Show loading spinner
            $btn.html('<i class="fas fa-spinner fa-spin fa-2x mb-2"></i><span>Đang tải...</span>');
            $btn.prop('disabled', true);
            
            // Allow navigation to proceed
            setTimeout(() => {
                // This will only execute if navigation fails
                $btn.html(originalHtml);
                $btn.prop('disabled', false);
            }, 3000);
        }
    });
}

// Đảm bảo biến toàn cục, chỉ khai báo 1 lần ở đầu file
let revenueChartInstance = null;

function initializeCharts() {
    const ctx = document.getElementById('revenueChart');
    if (!ctx) {
        console.warn('revenueChart canvas not found');
        return;
    }
    if (revenueChartInstance) {
        try {
            revenueChartInstance.destroy();
        } catch (e) {
            console.warn('Chart destroy error:', e);
        }
        revenueChartInstance = null;
    }
    try {
        const revenueData = window.revenueChartData || [];
        const revenueLabels = window.revenueChartLabels || [];
        revenueChartInstance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: revenueLabels,
                datasets: [{
                    label: 'Doanh thu',
                    data: revenueData,
                    backgroundColor: 'rgba(54, 162, 235, 0.5)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { display: false },
                    title: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    } catch (err) {
        console.error('Error creating chart:', err);
    }
}

function initializeCalendar() {
    const calendarEl = document.getElementById('calendar');
    if (!calendarEl) {
        console.warn('Calendar element not found');
        return;
    }
    
    if (typeof FullCalendar === 'undefined') {
        console.error('FullCalendar is not loaded');
        return;
    }

    try {
        const calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'dayGridMonth',
            locale: 'vi',
            height: 400,
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek'
            },
            events: window.calendarEvents || [],
            eventClick: function(info) {
                showBookingModal(info.event.id);
            },
            eventMouseEnter: function(info) {
                // Use Bootstrap tooltip if available
                if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                    new bootstrap.Tooltip(info.el, {
                        title: info.event.title + '<br>' + (info.event.extendedProps.customerName || ''),
                        html: true,
                        placement: 'top'
                    });
                } else {
                    // Fallback to jQuery tooltip
                    $(info.el).tooltip({
                        title: info.event.title + '<br>' + (info.event.extendedProps.customerName || ''),
                        html: true,
                        placement: 'top',
                        container: 'body'
                    });
                }
            },
            eventDidMount: function(info) {
                // Color coding based on booking status
                const status = info.event.extendedProps.status;
                switch(status) {
                    case 'confirmed':
                        info.el.style.backgroundColor = '#28a745';
                        break;
                    case 'pending':
                        info.el.style.backgroundColor = '#ffc107';
                        break;
                    case 'completed':
                        info.el.style.backgroundColor = '#17a2b8';
                        break;
                    case 'cancelled':
                        info.el.style.backgroundColor = '#dc3545';
                        break;
                    default:
                        info.el.style.backgroundColor = '#6c757d';
                }
            }
        });
        
        calendar.render();
        
        // Filter calendar by homestay
        $('#homestayFilter').change(function() {
            const homestayId = $(this).val();
            if (homestayId) {
                calendar.refetchEvents();
            }
        });
    } catch (error) {
        console.error('Error initializing calendar:', error);
    }
}

function showBookingModal(bookingId) {
    if (!bookingId) {
        console.error('Booking ID is required');
        return;
    }

    // Show loading modal
    $('#bookingModal').modal('show');
    $('#bookingModalBody').html('<div class="text-center"><i class="fas fa-spinner fa-spin fa-2x"></i><br>Đang tải...</div>');
    
    // Load booking details via AJAX
    $.get('/Host/GetBookingDetail/' + bookingId)
        .done(function(data) {
            $('#bookingModalBody').html(data);
            initializeModalActions(bookingId);
        })
        .fail(function(xhr) {
            const errorMessage = xhr.responseJSON?.message || 'Không thể tải thông tin đặt phòng.';
            $('#bookingModalBody').html(`<div class="alert alert-danger">${errorMessage}</div>`);
        });
}

function initializeModalActions(bookingId) {
    // Remove existing event handlers to prevent duplicates
    $('#confirmBookingBtn, #rejectBookingBtn, #checkinBookingBtn, #completeBookingBtn').off('click');
    
    // Confirm booking
    $('#confirmBookingBtn').on('click', function() {
        performBookingAction(bookingId, 'confirm', 'Xác nhận đặt phòng thành công!');
    });
    
    // Reject booking
    $('#rejectBookingBtn').on('click', function() {
        performBookingAction(bookingId, 'reject', 'Đã từ chối đặt phòng!');
    });
    
    // Check-in booking
    $('#checkinBookingBtn').on('click', function() {
        performBookingAction(bookingId, 'checkin', 'Check-in thành công!');
    });
    
    // Complete booking
    $('#completeBookingBtn').on('click', function() {
        performBookingAction(bookingId, 'complete', 'Hoàn thành đặt phòng!');
    });
}

function performBookingAction(bookingId, action, successMessage) {
    // Use event.target safely
    const $btn = $(event?.target || this);
    const originalText = $btn.text();
    
    // Show loading state
    $btn.html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');
    $btn.prop('disabled', true);
    
    $.post('/Host/' + capitalizeFirst(action) + 'Booking/' + bookingId)
        .done(function(response) {
            showSuccessToast(successMessage);
            $('#bookingModal').modal('hide');
            // Refresh calendar and stats
            setTimeout(() => {
                location.reload();
            }, 1000);
        })
        .fail(function(xhr) {
            const error = xhr.responseJSON?.message || 'Có lỗi xảy ra, vui lòng thử lại!';
            showErrorToast(error);
        })
        .always(function() {
            $btn.text(originalText);
            $btn.prop('disabled', false);
        });
}

function capitalizeFirst(str) {
    if (!str || typeof str !== 'string') return '';
    return str.charAt(0).toUpperCase() + str.slice(1);
}

function showSuccessToast(message) {
    if (!message) return;
    
    const toastId = 'toast-' + Date.now();
    const toast = $(`
        <div id="${toastId}" class="toast align-items-center text-white bg-success border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-check-circle me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `);
    
    const $container = getToastContainer();
    $container.append(toast);
    
    // Use Bootstrap 5 toast if available, otherwise fallback
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        const toastBootstrap = new bootstrap.Toast(toast[0]);
        toastBootstrap.show();
    } else {
        toast.toast('show');
    }
    
    setTimeout(() => {
        toast.remove();
    }, 5000);
}

function showErrorToast(message) {
    if (!message) return;
    
    const toastId = 'toast-' + Date.now();
    const toast = $(`
        <div id="${toastId}" class="toast align-items-center text-white bg-danger border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-exclamation-circle me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `);
    
    const $container = getToastContainer();
    $container.append(toast);
    
    // Use Bootstrap 5 toast if available, otherwise fallback
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        const toastBootstrap = new bootstrap.Toast(toast[0]);
        toastBootstrap.show();
    } else {
        toast.toast('show');
    }
    
    setTimeout(() => {
        toast.remove();
    }, 5000);
}

function getToastContainer() {
    let $container = $('#toastContainer');
    if ($container.length === 0) {
        $container = $('<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
        $('body').append($container);
    }
    return $container;
}

// Counter animation for stat cards
function animateCounters() {
    $('.fade-in h4').each(function() {
        const $this = $(this);
        const text = $this.text();
        
        // Check if it's a number (with or without currency symbols)
        const match = text.match(/[\d,]+/);
        if (match) {
            const number = parseInt(match[0].replace(/,/g, ''));
            if (!isNaN(number) && number > 0) {
                $this.prop('Counter', 0).animate({
                    Counter: number
                }, {
                    duration: 2000,
                    easing: 'swing',
                    step: function(now) {
                        const formatted = Math.ceil(now).toLocaleString('vi-VN');
                        $this.text(text.replace(/[\d,]+/, formatted));
                    },
                    complete: function() {
                        // Ensure final value is correct
                        const finalFormatted = number.toLocaleString('vi-VN');
                        $this.text(text.replace(/[\d,]+/, finalFormatted));
                    }
                });
            }
        }
    });
}

// Initialize counter animation after page load
$(window).on('load', function() {
    setTimeout(animateCounters, 500);
});

// Cleanup function for page unload
$(window).on('beforeunload', function() {
    if (revenueChartInstance) {
        try {
            revenueChartInstance.destroy();
        } catch (e) {
            console.warn('Chart cleanup error:', e);
        }
    }
});

// Initialize toast container
$(document).ready(function() {
    getToastContainer();
});