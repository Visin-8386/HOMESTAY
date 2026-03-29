import 'dart:math' as math;

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../services/admin_service.dart';
import '../../widgets/user_gradient_background.dart';

class AdminStatsScreen extends StatefulWidget {
  const AdminStatsScreen({super.key});

  @override
  State<AdminStatsScreen> createState() => _AdminStatsScreenState();
}

class _AdminStatsScreenState extends State<AdminStatsScreen> {
  final AdminService _admin = AdminService();
  Map<String, dynamic>? _stats;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    if (!mounted) return;
    setState(() => _loading = true);
    try {
      final data = await _admin.getStats();
      if (!mounted) return;
      setState(() {
        _stats = data as Map<String, dynamic>?;
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi khi tải thống kê: $e')));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Thống kê hệ thống')),
      body: UserGradientBackground(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _stats == null
                ? const Center(child: Text('Không có dữ liệu'))
                : SingleChildScrollView(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Thống kê tổng quan',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        LayoutBuilder(builder: (context, constraints) {
                          final max = constraints.maxWidth;
                          final isSmall = max < 600;
                          Widget statCard(String title, String value, IconData icon, Color color) {
                            return SizedBox(
                              width: isSmall ? max : (max - 24) / 3,
                              child: Card(
                                elevation: 4,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                child: Padding(
                                  padding: const EdgeInsets.all(12),
                                  child: Row(
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.all(10),
                                        decoration: BoxDecoration(
                                          color: color.withOpacity(0.12),
                                          borderRadius: BorderRadius.circular(8),
                                        ),
                                        child: Icon(icon, color: color, size: 24),
                                      ),
                                      const SizedBox(width: 12),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              title,
                                              style: const TextStyle(
                                                fontSize: 12,
                                                color: Colors.grey,
                                              ),
                                            ),
                                            Text(
                                              value,
                                              style: const TextStyle(
                                                fontSize: 16,
                                                fontWeight: FontWeight.bold,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            );
                          }

                          return Wrap(
                            spacing: 12,
                            runSpacing: 12,
                            children: [
                              statCard('Tổng người dùng', _stats!['totalUsers']?.toString() ?? '0', Icons.people, Colors.blue),
                              statCard('Tổng Homestay', _stats!['totalHomestays']?.toString() ?? '0', Icons.business, Colors.green),
                              statCard('Tổng đặt phòng', _stats!['totalBookings']?.toString() ?? '0', Icons.book, Colors.orange),
                            ],
                          );
                        }),
                        const SizedBox(height: 24),
                        const Text(
                          'Doanh thu theo tháng',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        SizedBox(
                          height: 200,
                          child: _buildRevenueChart(),
                        ),
                        const SizedBox(height: 24),
                        const Text(
                          'Chi tiết thống kê',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        Card(
                          elevation: 4,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Thống kê chi tiết',
                                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                                ),
                                const SizedBox(height: 12),
                                _buildStatRow('Người dùng đang hoạt động', _stats!['activeUsers']?.toString() ?? '0'),
                                _buildStatRow('Homestay đang hoạt động', _stats!['activeHomestays']?.toString() ?? '0'),
                                _buildStatRow('Đặt phòng hôm nay', _stats!['bookingsToday']?.toString() ?? '0'),
                                _buildStatRow('Doanh thu hôm nay', '${_stats!['revenueToday'] ?? 0} VND'),
                                _buildStatRow('Doanh thu tháng này', '${_stats!['revenueThisMonth'] ?? 0} VND'),
                                _buildStatRow('Doanh thu năm nay', '${_stats!['revenueThisYear'] ?? 0} VND'),
                                const SizedBox(height: 8),
                                Text('Generated at: ${_stats!['generatedAt'] ?? '-'}'),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        _buildChartsSection(),
                      ],
                    ),
                  ),
      ),
    );
  }

  Widget _buildRevenueChart() {
    final revenueTrend = (_stats!['revenueTrend'] as List<dynamic>?)
            ?.map((e) => (e is num) ? e.toDouble() : double.tryParse(e.toString()) ?? 0.0)
            .toList() ??
        [];

    final revenueSpots = <FlSpot>[];
    for (var i = 0; i < revenueTrend.length; i++) {
      revenueSpots.add(FlSpot(i.toDouble(), revenueTrend[i]));
    }

    return revenueSpots.isEmpty
        ? const Center(child: Text('Không có dữ liệu doanh thu'))
        : LineChart(
            LineChartData(
              gridData: FlGridData(show: true, drawVerticalLine: false),
              minY: _calcMinY(revenueSpots),
              maxY: _calcMaxY(revenueSpots),
              titlesData: FlTitlesData(
                leftTitles: AxisTitles(
                    sideTitles: SideTitles(
                  showTitles: true,
                  interval: _calcInterval(_calcMinY(revenueSpots), _calcMaxY(revenueSpots)),
                  getTitlesWidget: (value, meta) => Padding(
                    padding: const EdgeInsets.only(right: 6.0),
                    child: Text(_formatCurrency(value), style: const TextStyle(fontSize: 10)),
                  ),
                )),
                bottomTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
              ),
              borderData: FlBorderData(show: false),
              lineBarsData: [
                LineChartBarData(spots: revenueSpots, isCurved: true, color: Colors.purple, barWidth: 3, dotData: FlDotData(show: false)),
              ],
            ),
          );
  }

  Widget _buildStatRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _buildChartsSection() {
    final revenueTrend = (_stats!['revenueTrend'] as List<dynamic>?)
            ?.map((e) => (e is num) ? e.toDouble() : double.tryParse(e.toString()) ?? 0.0)
            .toList() ??
        [];

    final bookingsByDay = (_stats!['bookingsByDay'] as Map<String, dynamic>?) ?? {};

    // Prepare sample x values for revenueTrend
    final revenueSpots = <FlSpot>[];
    for (var i = 0; i < revenueTrend.length; i++) {
      revenueSpots.add(FlSpot(i.toDouble(), revenueTrend[i]));
    }

    final barGroups = <BarChartGroupData>[];
    final keys = bookingsByDay.keys.toList();
    final counts = keys.map((k) {
      final v = bookingsByDay[k];
      return (v is num) ? v.toDouble() : double.tryParse(v.toString()) ?? 0.0;
    }).toList();

    // choose bar width based on number of bars to avoid overlap
  final barWidth = keys.length > 12 ? 8.0 : (keys.length > 6 ? 10.0 : 14.0);

    for (var i = 0; i < keys.length; i++) {
      barGroups.add(BarChartGroupData(x: i, barRods: [BarChartRodData(toY: counts[i], width: barWidth, color: Colors.blue)]));
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Biểu đồ', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        // Revenue line chart
        Card(
          elevation: 3,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: SizedBox(
              height: 180,
              child: revenueSpots.isEmpty
                  ? const Center(child: Text('Không có dữ liệu doanh thu'))
                  : LineChart(
                      LineChartData(
                        gridData: FlGridData(show: true, drawVerticalLine: false),
                        // compute nice min/max and interval for Y axis to avoid autoscaling jumps
                        minY: _calcMinY(revenueSpots),
                        maxY: _calcMaxY(revenueSpots),
                        titlesData: FlTitlesData(
                          leftTitles: AxisTitles(
                              sideTitles: SideTitles(
                            showTitles: true,
                            interval: _calcInterval(_calcMinY(revenueSpots), _calcMaxY(revenueSpots)),
                            getTitlesWidget: (value, meta) => Padding(
                              padding: const EdgeInsets.only(right: 6.0),
                              child: Text(_formatCurrency(value), style: const TextStyle(fontSize: 10)),
                            ),
                          )),
                          bottomTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        ),
                        borderData: FlBorderData(show: false),
                        lineBarsData: [
                          LineChartBarData(spots: revenueSpots, isCurved: true, color: Colors.purple, barWidth: 3, dotData: FlDotData(show: false)),
                        ],
                      ),
                    ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        // Bookings bar chart
        Card(
          elevation: 3,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: SizedBox(
              height: 200,
              child: barGroups.isEmpty
                  ? const Center(child: Text('Không có dữ liệu đặt phòng theo ngày'))
                  : BarChart(
                      BarChartData(
                        alignment: BarChartAlignment.spaceBetween,
                        maxY: _calcMaxYFromList(counts),
                        titlesData: FlTitlesData(
                          show: true,
                          bottomTitles: AxisTitles(
                            sideTitles: SideTitles(
                              showTitles: true,
                              getTitlesWidget: (v, meta) {
                                final idx = v.toInt();
                                if (idx < 0 || idx >= keys.length) return const SizedBox.shrink();

                                // show only a subset of labels to avoid overlap
                                final maxLabels = 6;
                                final step = keys.length > maxLabels ? (keys.length ~/ maxLabels) : 1;
                                if (idx % step != 0) return const SizedBox.shrink();

                                final key = keys[idx];
                                return Padding(
                                  padding: const EdgeInsets.only(top: 4.0),
                                  child: Transform.rotate(
                                    angle: -math.pi / 8,
                                    child: Text(key, style: const TextStyle(fontSize: 10)),
                                  ),
                                );
                              },
                            ),
                          ),
                          leftTitles: AxisTitles(
                              sideTitles: SideTitles(
                            showTitles: true,
                            interval: _calcInterval(0, _calcMaxYFromList(counts)),
                            getTitlesWidget: (value, meta) => Padding(
                              padding: const EdgeInsets.only(right: 6.0),
                              child: Text(value.toInt().toString(), style: const TextStyle(fontSize: 10)),
                            ),
                          )),
                        ),
                        borderData: FlBorderData(show: false),
                        barGroups: barGroups,
                      ),
                    ),
            ),
          ),
        ),
      ],
    );
  }

  // Helpers to stabilize chart scales and labels
  double _calcMinY(List<FlSpot> spots) {
    if (spots.isEmpty) return 0.0;
    final minVal = spots.map((s) => s.y).reduce(math.min);
    // For revenues we prefer baseline at 0; otherwise give a small padding below min
    if (minVal > 0) return 0.0;
    return minVal * 0.9;
  }

  double _calcMaxY(List<FlSpot> spots) {
    if (spots.isEmpty) return 1.0;
    final maxVal = spots.map((s) => s.y).reduce(math.max);
    if (maxVal <= 0) return 1.0;
    return maxVal * 1.15; // 15% headroom
  }

  double _calcMaxYFromList(List<double> values) {
    if (values.isEmpty) return 1.0;
    final m = values.reduce(math.max);
    if (m <= 0) return 1.0;
    return (m * 1.2).ceilToDouble();
  }

  double _calcInterval(double minY, double maxY) {
    final range = (maxY - minY).abs();
    if (range <= 0) return 1.0;
    // target ~4 steps
    final raw = range / 4.0;
    final magnitude = math.pow(10, (math.log(raw) / math.ln10).floor()).toDouble();
    var nice = (raw / magnitude).round() * magnitude;
    if (nice == 0) nice = magnitude;
    return nice;
  }

  String _formatCurrency(double v) {
    if (v >= 1000000) {
      return '${(v / 1000000).toStringAsFixed(1)}M';
    }
    if (v >= 1000) {
      return '${(v / 1000).toStringAsFixed(0)}k';
    }
    return v.toInt().toString();
  }
}
