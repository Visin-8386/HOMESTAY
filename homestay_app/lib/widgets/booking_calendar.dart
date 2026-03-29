import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';

import '../../config/app_colors.dart';
import '../../services/booking_service.dart';

class BookingCalendar extends StatefulWidget {
  final int homestayId;
  final DateTime? initialCheckIn;
  final DateTime? initialCheckOut;
  final Function(DateTime?, DateTime?) onDateRangeSelected;

  const BookingCalendar({
    super.key,
    required this.homestayId,
    this.initialCheckIn,
    this.initialCheckOut,
    required this.onDateRangeSelected,
  });

  @override
  State<BookingCalendar> createState() => _BookingCalendarState();
}

class _BookingCalendarState extends State<BookingCalendar> {
  late DateTime _focusedDay;
  DateTime? _selectedDay;
  DateTime? _rangeStart;
  DateTime? _rangeEnd;
  List<DateTime> _bookedDates = [];
  List<DateTime> _blockedDates = [];
  bool _isLoading = true;

  final BookingService _bookingService = BookingService();

  @override
  void initState() {
    super.initState();
    _focusedDay = DateTime.now();
    _rangeStart = widget.initialCheckIn;
    _rangeEnd = widget.initialCheckOut;
    _loadUnavailableDates();
  }

  Future<void> _loadUnavailableDates() async {
    try {
      setState(() => _isLoading = true);

      // Load booked dates
      final bookedDates = await _bookingService.getBookedDates(widget.homestayId);
      setState(() => _bookedDates = bookedDates);

      // TODO: Load blocked dates when API is available
      // For now, we'll assume no blocked dates from host side in mobile app
      // Host can block dates from web interface

    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Không thể tải thông tin ngày: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  bool _isDateUnavailable(DateTime date) {
    final dateOnly = DateTime(date.year, date.month, date.day);
    return _bookedDates.any((booked) =>
        booked.year == dateOnly.year &&
        booked.month == dateOnly.month &&
        booked.day == dateOnly.day) ||
        _blockedDates.any((blocked) =>
        blocked.year == dateOnly.year &&
        blocked.month == dateOnly.month &&
        blocked.day == dateOnly.day);
  }

  bool _isDateBooked(DateTime date) {
    final dateOnly = DateTime(date.year, date.month, date.day);
    return _bookedDates.any((booked) =>
        booked.year == dateOnly.year &&
        booked.month == dateOnly.month &&
        booked.day == dateOnly.day);
  }

  bool _isDateBlocked(DateTime date) {
    final dateOnly = DateTime(date.year, date.month, date.day);
    return _blockedDates.any((blocked) =>
        blocked.year == dateOnly.year &&
        blocked.month == dateOnly.month &&
        blocked.day == dateOnly.day);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Legend
        Container(
          padding: const EdgeInsets.all(12),
          margin: const EdgeInsets.only(bottom: 8),
          decoration: BoxDecoration(
            color: Colors.grey[100],
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              _buildLegendItem('Đã đặt', Colors.green),
              const SizedBox(width: 16),
              _buildLegendItem('Đã khóa', Colors.grey),
              const SizedBox(width: 16),
              _buildLegendItem('Khả dụng', Colors.white, border: true),
            ],
          ),
        ),

        // Calendar
        Container(
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey[300]!),
            borderRadius: BorderRadius.circular(8),
          ),
          child: _isLoading
              ? const SizedBox(
                  height: 300,
                  child: Center(child: CircularProgressIndicator()),
                )
              : TableCalendar(
                  firstDay: DateTime.now(),
                  lastDay: DateTime.now().add(const Duration(days: 365)),
                  focusedDay: _focusedDay,
                  selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
                  rangeStartDay: _rangeStart,
                  rangeEndDay: _rangeEnd,
                  rangeSelectionMode: RangeSelectionMode.toggledOn,
                  onDaySelected: (selectedDay, focusedDay) {
                    if (!isSameDay(_selectedDay, selectedDay)) {
                      setState(() {
                        _selectedDay = selectedDay;
                        _focusedDay = focusedDay;
                        _rangeStart = null;
                        _rangeEnd = null;
                      });
                      widget.onDateRangeSelected(null, null);
                    }
                  },
                  onRangeSelected: (start, end, focusedDay) {
                    // If both start and end are selected, validate that the whole
                    // inclusive range does not contain any unavailable dates.
                    bool invalidRange = false;
                    if (start != null && end != null) {
                      DateTime cursor = DateTime(start.year, start.month, start.day);
                      final DateTime endOnly = DateTime(end.year, end.month, end.day);
                      while (!cursor.isAfter(endOnly)) {
                        if (_isDateUnavailable(cursor)) {
                          invalidRange = true;
                          break;
                        }
                        cursor = cursor.add(const Duration(days: 1));
                      }
                    }

                    if (invalidRange) {
                      // Reject selection that includes any unavailable date
                      if (mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('Khoảng ngày chọn có ngày đã bị đặt/khóa. Vui lòng chọn khoảng khác.'),
                            duration: Duration(seconds: 2),
                          ),
                        );
                      }
                      setState(() {
                        _selectedDay = null;
                        _rangeStart = null;
                        _rangeEnd = null;
                        _focusedDay = focusedDay;
                      });
                      widget.onDateRangeSelected(null, null);
                      return;
                    }

                    setState(() {
                      _selectedDay = null;
                      _rangeStart = start;
                      _rangeEnd = end;
                      _focusedDay = focusedDay;
                    });
                    widget.onDateRangeSelected(start, end);
                  },
                  enabledDayPredicate: (day) {
                    return !_isDateUnavailable(day);
                  },
                  calendarStyle: CalendarStyle(
                    todayDecoration: BoxDecoration(
                      color: AppColors.primary.withAlpha((0.3 * 255).round()),
                      shape: BoxShape.circle,
                    ),
                    selectedDecoration: BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                    rangeStartDecoration: BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                    rangeEndDecoration: BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                    rangeHighlightColor: AppColors.primary.withAlpha((0.1 * 255).round()),
                    disabledTextStyle: const TextStyle(
                      color: Colors.grey,
                      decoration: TextDecoration.lineThrough,
                    ),
                    outsideTextStyle: const TextStyle(color: Colors.grey),
                  ),
                  headerStyle: HeaderStyle(
                    formatButtonVisible: false,
                    titleCentered: true,
                    titleTextStyle: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  calendarBuilders: CalendarBuilders(
                    defaultBuilder: (context, day, focusedDay) {
                      if (_isDateBooked(day)) {
                        return Container(
                          margin: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: Colors.green[100],
                            border: Border.all(color: Colors.green[300]!),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Center(
                            child: Text(
                              '${day.day}',
                              style: const TextStyle(color: Colors.green),
                            ),
                          ),
                        );
                      } else if (_isDateBlocked(day)) {
                        return Container(
                          margin: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: Colors.grey[100],
                            border: Border.all(color: Colors.grey[400]!),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Center(
                            child: Text(
                              '${day.day}',
                              style: const TextStyle(color: Colors.grey),
                            ),
                          ),
                        );
                      }
                      return null;
                    },
                  ),
                ),
        ),

        // Selected dates info
        if (_rangeStart != null || _rangeEnd != null)
          Container(
            padding: const EdgeInsets.all(12),
            margin: const EdgeInsets.only(top: 8),
            decoration: BoxDecoration(
              color: AppColors.primary.withAlpha((0.1 * 255).round()),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              children: [
                if (_rangeStart != null)
                  Text(
                    'Ngày nhận: ${DateFormat('dd/MM/yyyy').format(_rangeStart!)}',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                if (_rangeEnd != null)
                  Text(
                    'Ngày trả: ${DateFormat('dd/MM/yyyy').format(_rangeEnd!)}',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                if (_rangeStart != null && _rangeEnd != null)
                  Text(
                    'Số đêm: ${(_rangeEnd!.difference(_rangeStart!).inDays)}',
                    style: TextStyle(color: Colors.grey[600]),
                  ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildLegendItem(String label, Color color, {bool border = false}) {
    return Row(
      children: [
        Container(
          width: 16,
          height: 16,
          decoration: BoxDecoration(
            color: color,
            border: border ? Border.all(color: Colors.grey[400]!) : null,
            borderRadius: BorderRadius.circular(2),
          ),
        ),
        const SizedBox(width: 4),
        Text(label, style: const TextStyle(fontSize: 12)),
      ],
    );
  }
}