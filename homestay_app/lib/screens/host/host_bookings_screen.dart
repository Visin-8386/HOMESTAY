import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../models/booking.dart';
import '../../providers/booking_provider.dart';

class HostBookingsScreen extends StatefulWidget {
  const HostBookingsScreen({super.key});

  @override
  State<HostBookingsScreen> createState() => _HostBookingsScreenState();
}

class _HostBookingsScreenState extends State<HostBookingsScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  
  @override
  void initState() {
    super.initState();
  _tabController = TabController(length: 4, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = Provider.of<BookingProvider>(context, listen: false);
      provider.loadHostBookings();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Quản lý đặt phòng'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
          bottom: TabBar(
          controller: _tabController,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          indicatorColor: Colors.white,
          tabs: const [
            Tab(text: 'Tất cả'),
            Tab(text: 'Đã hủy'),
            Tab(text: 'Đã thanh toán'),
            Tab(text: 'Đã hoàn thành'),
          ],
        ),
      ),
      body: Consumer<BookingProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (provider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.error_outline, size: 64, color: Colors.red[300]),
                  const SizedBox(height: 16),
                  Text(
                    provider.error!,
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Colors.grey[600]),
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () => provider.loadHostBookings(),
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            );
          }

          if (provider.hostBookings.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.book_outlined, size: 80, color: Colors.grey[400]),
                  const SizedBox(height: 16),
                  Text(
                    'Chưa có đặt phòng nào',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: Colors.grey[600],
                    ),
                  ),
                ],
              ),
            );
          }

          return TabBarView(
            controller: _tabController,
            children: [
              // Tất cả
              _buildBookingList(provider.hostBookings),
              // Đã hủy
              _buildBookingList(provider.hostBookings.where((b) => b.status == 'Cancelled').toList()),
              // Đã thanh toán (map to Confirmed or isPaid flag if present)
              _buildBookingList(provider.hostBookings.where((b) {
                // If the booking model later includes an isPaid field, prefer it.
                final isPaidField = (b as dynamic);
                try {
                  if (isPaidField.isPaid != null) return isPaidField.isPaid == true;
                } catch (_) {}
                // Accept either 'Paid' or 'Confirmed' (some backends use Confirmed)
                return b.status == 'Confirmed' || b.status == 'Paid';
              }).toList()),
              // Đã hoàn thành
              _buildBookingList(provider.hostBookings.where((b) => b.status == 'Completed').toList()),
            ],
          );
        },
      ),
    );
  }

  Widget _buildBookingList(List<Booking> bookings) {
    if (bookings.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.inbox_outlined, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'Không có đặt phòng nào',
              style: TextStyle(color: Colors.grey[600]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () {
        final provider = Provider.of<BookingProvider>(context, listen: false);
        return provider.loadHostBookings();
      },
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: bookings.length,
        itemBuilder: (context, index) {
          final booking = bookings[index];
          return _buildBookingCard(booking);
        },
      ),
    );
  }

  Widget _buildBookingCard(Booking booking) {
    Color statusColor;
    IconData statusIcon;
    
    switch (booking.status) {
      case 'Pending':
        statusColor = AppColors.pending;
        statusIcon = Icons.schedule;
        break;
      case 'Confirmed':
        statusColor = AppColors.confirmed;
        statusIcon = Icons.check_circle;
        break;
      case 'Cancelled':
        statusColor = AppColors.cancelled;
        statusIcon = Icons.cancel;
        break;
      case 'Completed':
        statusColor = AppColors.success;
        statusIcon = Icons.done_all;
        break;
      default:
        statusColor = Colors.grey;
        statusIcon = Icons.help_outline;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        onTap: () => _showBookingDetails(booking),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          booking.homestayName,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Mã: #${booking.id}',
                          style: TextStyle(
                            fontSize: 13,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: statusColor.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(statusIcon, size: 16, color: statusColor),
                        const SizedBox(width: 4),
                        Text(
                          booking.statusDisplay,
                          style: TextStyle(
                            color: statusColor,
                            fontWeight: FontWeight.bold,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const Divider(height: 24),
              
              // Guest Info
              _buildInfoRow(Icons.person, 'Khách', booking.guestName),
              
              const SizedBox(height: 8),
              
              // Booking Info
              _buildInfoRow(Icons.calendar_today, 'Nhận phòng', 
                '${booking.checkInDate.day}/${booking.checkInDate.month}/${booking.checkInDate.year}'),
              _buildInfoRow(Icons.calendar_today, 'Trả phòng', 
                '${booking.checkOutDate.day}/${booking.checkOutDate.month}/${booking.checkOutDate.year}'),
              _buildInfoRow(Icons.people, 'Số khách', '${booking.numberOfGuests} người'),
              
              const SizedBox(height: 8),
              
              // Price
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Tổng tiền:',
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.grey[700],
                    ),
                  ),
                  Text(
                    '${booking.totalPrice.toStringAsFixed(0)}đ',
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ],
              ),
              
              // Actions
              if (booking.status == 'Pending') ...[
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => _rejectBooking(booking),
                        icon: const Icon(Icons.close, size: 18),
                        label: const Text('Từ chối'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.red,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: ElevatedButton.icon(
                        onPressed: () => _confirmBooking(booking),
                        icon: const Icon(Icons.check, size: 18),
                        label: const Text('Xác nhận'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Colors.green,
                          foregroundColor: Colors.white,
                        ),
                      ),
                    ),
                  ],
                ),
              ] else if (booking.status == 'Confirmed') ...[
                const SizedBox(height: 12),
                Center(
                  child: OutlinedButton.icon(
                    onPressed: () => _cancelBooking(booking),
                    icon: const Icon(Icons.cancel, size: 18),
                    label: const Text('Hủy đặt phòng'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: Colors.red,
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Icon(icon, size: 16, color: AppColors.textSecondary),
          const SizedBox(width: 8),
          Text(
            '$label: ',
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontWeight: FontWeight.w500,
                fontSize: 13,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _showBookingDetails(Booking booking) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        minChildSize: 0.5,
        maxChildSize: 0.95,
        expand: false,
        builder: (context, scrollController) => Container(
          padding: const EdgeInsets.all(24),
          child: ListView(
            controller: scrollController,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Chi tiết đặt phòng',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
              const Divider(height: 24),
              
              _buildDetailSection('Thông tin homestay', [
                _buildDetailRow('Tên homestay', booking.homestayName),
                _buildDetailRow('Mã đặt phòng', '#${booking.id}'),
              ]),
              
              const Divider(height: 24),
              
              _buildDetailSection('Thông tin khách hàng', [
                _buildDetailRow('Họ tên', booking.guestName),
                _buildDetailRow('User ID', booking.userId),
              ]),
              
              const Divider(height: 24),
              
              _buildDetailSection('Thông tin đặt phòng', [
                _buildDetailRow('Ngày nhận phòng', 
                  '${booking.checkInDate.day}/${booking.checkInDate.month}/${booking.checkInDate.year}'),
                _buildDetailRow('Ngày trả phòng', 
                  '${booking.checkOutDate.day}/${booking.checkOutDate.month}/${booking.checkOutDate.year}'),
                _buildDetailRow('Số đêm', '${booking.checkOutDate.difference(booking.checkInDate).inDays} đêm'),
                _buildDetailRow('Số khách', '${booking.numberOfGuests} người'),
                _buildDetailRow('Trạng thái', booking.statusDisplay),
              ]),
              
              if (booking.specialRequests != null && booking.specialRequests!.isNotEmpty) ...[
                const Divider(height: 24),
                _buildDetailSection('Yêu cầu đặc biệt', [
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(booking.specialRequests!),
                  ),
                ]),
              ],
              
              const Divider(height: 24),
              
              _buildDetailSection('Thanh toán', [
                _buildDetailRow('Tổng tiền', '${booking.totalPrice.toStringAsFixed(0)}đ', 
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: AppColors.primary,
                  ),
                ),
              ]),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailSection(String title, List<Widget> children) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 12),
        ...children,
      ],
    );
  }

  Widget _buildDetailRow(String label, String value, {TextStyle? style}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(
              label,
              style: TextStyle(color: Colors.grey[600]),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: style ?? const TextStyle(fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmBooking(Booking booking) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xác nhận đặt phòng'),
        content: Text('Bạn có chắc muốn xác nhận đặt phòng #${booking.id}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
            child: const Text('Xác nhận'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    final provider = Provider.of<BookingProvider>(context, listen: false);
    final success = await provider.updateBookingStatus(booking.id, 'Confirmed');

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Đã xác nhận đặt phòng'),
          backgroundColor: Colors.green,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(provider.error ?? 'Xác nhận thất bại'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> _rejectBooking(Booking booking) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Từ chối đặt phòng'),
        content: Text('Bạn có chắc muốn từ chối đặt phòng #${booking.id}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Từ chối'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    final provider = Provider.of<BookingProvider>(context, listen: false);
    final success = await provider.updateBookingStatus(booking.id, 'Cancelled');

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Đã từ chối đặt phòng'),
          backgroundColor: Colors.orange,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(provider.error ?? 'Từ chối thất bại'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> _cancelBooking(Booking booking) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Hủy đặt phòng'),
        content: Text('Bạn có chắc muốn hủy đặt phòng #${booking.id}? Khách hàng sẽ được thông báo.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Không'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Hủy đặt phòng'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    final provider = Provider.of<BookingProvider>(context, listen: false);
    final success = await provider.cancelBooking(booking.id);

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Đã hủy đặt phòng'),
          backgroundColor: Colors.orange,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(provider.error ?? 'Hủy thất bại'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }
}
