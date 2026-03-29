import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../config/app_colors.dart';
import '../../services/payment_service.dart';

class PaymentHistoryScreen extends StatefulWidget {
  const PaymentHistoryScreen({Key? key}) : super(key: key);

  @override
  State<PaymentHistoryScreen> createState() => _PaymentHistoryScreenState();
}

class _PaymentHistoryScreenState extends State<PaymentHistoryScreen> {
  final PaymentService _paymentService = PaymentService();
  final TextEditingController _searchController = TextEditingController();
  
  List<Map<String, dynamic>> _payments = [];
  List<Map<String, dynamic>> _filteredPayments = [];
  bool _isLoading = true;
  String? _error;
  
  String? _selectedStatus;
  DateTimeRange? _dateRange;
  
  final List<String> _statuses = ['All', 'Pending', 'Completed', 'Failed', 'Refunded'];

  @override
  void initState() {
    super.initState();
    _loadPayments();
    _searchController.addListener(_filterPayments);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadPayments() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final payments = await _paymentService.getPaymentHistory(
        status: _selectedStatus != null && _selectedStatus != 'All' ? _selectedStatus : null,
        startDate: _dateRange?.start,
        endDate: _dateRange?.end,
      );
      
      setState(() {
        _payments = payments;
        _filteredPayments = payments;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  void _filterPayments() {
    final query = _searchController.text.toLowerCase();
    setState(() {
      _filteredPayments = _payments.where((payment) {
        final bookingRef = (payment['bookingReference'] ?? '').toString().toLowerCase();
        final method = (payment['method'] ?? '').toString().toLowerCase();
        return bookingRef.contains(query) || method.contains(query);
      }).toList();
    });
  }

  Future<void> _selectDateRange() async {
    final DateTimeRange? picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
      initialDateRange: _dateRange,
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: ColorScheme.light(
              primary: AppColors.primary,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null && picked != _dateRange) {
      setState(() {
        _dateRange = picked;
      });
      _loadPayments();
    }
  }

  Future<void> _exportReport(String format) async {
    try {
      final url = await _paymentService.exportPaymentReport(
        startDate: _dateRange?.start,
        endDate: _dateRange?.end,
        format: format,
      );
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đã tạo báo cáo: $url'),
            backgroundColor: Colors.green,
            action: SnackBarAction(
              label: 'Mở',
              textColor: Colors.white,
              onPressed: () {
                // TODO: Open URL
              },
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lịch sử thanh toán'),
        backgroundColor: AppColors.primary,
        actions: [
          PopupMenuButton<String>(
            icon: const Icon(Icons.download),
            onSelected: _exportReport,
            itemBuilder: (context) => [
              const PopupMenuItem(
                value: 'pdf',
                child: Row(
                  children: [
                    Icon(Icons.picture_as_pdf, color: Colors.red),
                    SizedBox(width: 8),
                    Text('Xuất PDF'),
                  ],
                ),
              ),
              const PopupMenuItem(
                value: 'csv',
                child: Row(
                  children: [
                    Icon(Icons.table_chart, color: Colors.green),
                    SizedBox(width: 8),
                    Text('Xuất CSV'),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          _buildFilterSection(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildFilterSection() {
    return Container(
      padding: const EdgeInsets.all(16),
      color: Colors.grey[100],
      child: Column(
        children: [
          // Search bar
          TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: 'Tìm kiếm theo mã booking hoặc phương thức...',
              prefixIcon: const Icon(Icons.search),
              filled: true,
              fillColor: Colors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: BorderSide.none,
              ),
              contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            ),
          ),
          const SizedBox(height: 12),
          // Filters row
          Row(
            children: [
              // Status filter
              Expanded(
                child: DropdownButtonFormField<String>(
                  value: _selectedStatus,
                  decoration: InputDecoration(
                    labelText: 'Trạng thái',
                    filled: true,
                    fillColor: Colors.white,
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  ),
                  items: _statuses.map((status) {
                    return DropdownMenuItem(
                      value: status,
                      child: Text(status == 'All' ? 'Tất cả' : status),
                    );
                  }).toList(),
                  onChanged: (value) {
                    setState(() {
                      _selectedStatus = value;
                    });
                    _loadPayments();
                  },
                ),
              ),
              const SizedBox(width: 12),
              // Date range button
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: _selectDateRange,
                  icon: const Icon(Icons.calendar_today, size: 18),
                  label: Text(
                    _dateRange != null
                        ? '${DateFormat('dd/MM').format(_dateRange!.start)} - ${DateFormat('dd/MM').format(_dateRange!.end)}'
                        : 'Chọn ngày',
                    style: const TextStyle(fontSize: 12),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.white,
                    foregroundColor: AppColors.textPrimary,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                      side: BorderSide(color: Colors.grey[300]!),
                    ),
                  ),
                ),
              ),
              if (_dateRange != null) ...[
                const SizedBox(width: 8),
                IconButton(
                  onPressed: () {
                    setState(() {
                      _dateRange = null;
                    });
                    _loadPayments();
                  },
                  icon: const Icon(Icons.clear),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.red),
            const SizedBox(height: 16),
            Text('Lỗi: $_error'),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadPayments,
              child: const Text('Thử lại'),
            ),
          ],
        ),
      );
    }

    if (_filteredPayments.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.payment, size: 80, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              _searchController.text.isNotEmpty
                  ? 'Không tìm thấy giao dịch'
                  : 'Chưa có giao dịch nào',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    color: Colors.grey[700],
                  ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadPayments,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _filteredPayments.length,
        itemBuilder: (context, index) {
          final payment = _filteredPayments[index];
          return _buildPaymentCard(payment);
        },
      ),
    );
  }

  Widget _buildPaymentCard(Map<String, dynamic> payment) {
    double amount = 0.0;
    try {
      final rawAmount = payment['amount'];
      if (rawAmount is String) {
        amount = double.tryParse(rawAmount) ?? 0.0;
      } else if (rawAmount is int) {
        amount = rawAmount.toDouble();
      } else if (rawAmount is double) {
        amount = rawAmount;
      }
    } catch (_) {
      amount = 0.0;
    }

    final method = payment['method']?.toString() ?? 'N/A';
    final status = payment['status']?.toString() ?? 'pending';
    final bookingReference = payment['bookingReference']?.toString() ?? 'N/A';

    DateTime? createdAt;
    try {
      final raw = payment['createdAt'];
      if (raw is DateTime) createdAt = raw;
      else if (raw is String && raw.isNotEmpty) createdAt = DateTime.parse(raw);
    } catch (_) {
      createdAt = null;
    }

    Color statusColor;
    IconData statusIcon;
    String statusText;

    switch (status.toLowerCase()) {
      case 'completed':
        statusColor = Colors.green;
        statusIcon = Icons.check_circle;
        statusText = 'Thành công';
        break;
      case 'failed':
        statusColor = Colors.red;
        statusIcon = Icons.cancel;
        statusText = 'Thất bại';
        break;
      case 'refunded':
        statusColor = Colors.orange;
        statusIcon = Icons.replay;
        statusText = 'Đã hoàn tiền';
        break;
      default:
        statusColor = Colors.blue;
        statusIcon = Icons.pending;
        statusText = 'Đang xử lý';
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () {
          _showPaymentDetail(payment);
        },
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${NumberFormat.currency(locale: 'vi', symbol: '₫').format(amount)}',
                          style: Theme.of(context).textTheme.titleLarge?.copyWith(
                                fontWeight: FontWeight.bold,
                                color: AppColors.primary,
                              ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Mã booking: $bookingReference',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
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
                          statusText,
                          style: TextStyle(
                            color: statusColor,
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const Divider(height: 24),
              Row(
                children: [
                  _buildInfoItem(Icons.payment, 'Phương thức', method),
                  const SizedBox(width: 24),
                  if (createdAt != null)
                    _buildInfoItem(
                      Icons.access_time,
                      'Ngày giao dịch',
                      DateFormat('dd/MM/yyyy').format(createdAt),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoItem(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 16, color: Colors.grey[600]),
        const SizedBox(width: 6),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: TextStyle(
                fontSize: 11,
                color: Colors.grey[600],
              ),
            ),
            Text(
              value,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ],
    );
  }

  void _showPaymentDetail(Map<String, dynamic> payment) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) {
        return DraggableScrollableSheet(
          initialChildSize: 0.6,
          minChildSize: 0.4,
          maxChildSize: 0.9,
          expand: false,
          builder: (context, scrollController) {
            return SingleChildScrollView(
              controller: scrollController,
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 4,
                      decoration: BoxDecoration(
                        color: Colors.grey[300],
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  Text(
                    'Chi tiết giao dịch',
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                  ),
                  const SizedBox(height: 24),
                  _buildDetailRow('Số tiền', 
                    NumberFormat.currency(locale: 'vi', symbol: '₫').format(payment['amount'])),
                  _buildDetailRow('Phương thức', payment['method'] ?? 'N/A'),
                  _buildDetailRow('Trạng thái', payment['status'] ?? 'N/A'),
                  _buildDetailRow('Mã booking', payment['bookingReference'] ?? 'N/A'),
                  if (payment['createdAt'] != null)
                    _buildDetailRow('Ngày tạo',
                      DateFormat('dd/MM/yyyy HH:mm').format(DateTime.parse(payment['createdAt']))),
                  if (payment['description'] != null)
                    _buildDetailRow('Mô tả', payment['description']),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: TextStyle(
                color: Colors.grey[600],
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
