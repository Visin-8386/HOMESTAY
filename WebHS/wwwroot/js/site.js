// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Admin Message Request Function
function requestAdminMessage() {
    $('#adminMessageModal').modal('show');
}

function sendAdminMessageRequest() {
    const messageText = $('#adminMessageText').val();
    if (!messageText || messageText.trim() === '') {
        alert('Vui lòng nhập nội dung tin nhắn!');
        return;
    }

    // Send AJAX request to request admin message
    $.ajax({
        url: '/Messaging/RequestAdminMessage',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            message: messageText.trim()
        }),
        success: function(response) {
            if (response.success) {
                $('#adminMessageModal').modal('hide');
                $('#adminMessageText').val('');
                alert('Đã gửi yêu cầu thành công! Admin sẽ phản hồi bạn sớm.');
                // Optionally redirect to messaging page
                // window.location.href = '/Messaging';
            } else {
                alert('Có lỗi xảy ra: ' + (response.message || 'Vui lòng thử lại!'));
            }
        },
        error: function(xhr, status, error) {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại!');
        }
    });
}

// Update unread message counts
function updateUnreadMessageCounts() {
    $.get('/Messaging/GetUnreadCounts', function(data) {
        if (data.success && data.totalUnread > 0) {
            $('#unread-messages-badge').text(data.totalUnread).show();
            $('#unreadMessagesCount').text(data.totalUnread).show();
        } else {
            $('#unread-messages-badge').hide();
            $('#unreadMessagesCount').hide();
        }
        
        if (data.success && data.adminRequests > 0) {
            $('#admin-requests-badge').text(data.adminRequests).show();
        } else {
            $('#admin-requests-badge').hide();
        }
    });
}

// Initialize on document ready
$(function() {
    // Update message counts if user is authenticated
    if ($('#unread-messages-badge').length > 0) {
        updateUnreadMessageCounts();
        // Update every 30 seconds
        setInterval(updateUnreadMessageCounts, 30000);
    }
});
