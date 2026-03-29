import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../services/homestay_service.dart';
import '../homestay/homestay_detail_screen.dart';

class MapViewScreen extends StatefulWidget {
  final String? city;

  const MapViewScreen({Key? key, this.city}) : super(key: key);

  @override
  State<MapViewScreen> createState() => _MapViewScreenState();
}

class _MapViewScreenState extends State<MapViewScreen> {
  final HomestayService _homestayService = HomestayService();
  GoogleMapController? _mapController;
  List<Homestay> _homestays = [];
  Set<Marker> _markers = {};
  bool _isLoading = true;
  LatLng _initialPosition = const LatLng(21.0285, 105.8542); // Hanoi default
  Homestay? _selectedHomestay;

  @override
  void initState() {
    super.initState();
    _getCurrentLocation();
    _loadHomestays();
  }

  Future<void> _getCurrentLocation() async {
    try {
      final permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        await Geolocator.requestPermission();
      }

      final position = await Geolocator.getCurrentPosition();
      setState(() {
        _initialPosition = LatLng(position.latitude, position.longitude);
      });
      
      _mapController?.animateCamera(
        CameraUpdate.newLatLng(_initialPosition),
      );
    } catch (e) {
      print('Error getting location: $e');
    }
  }

  Future<void> _loadHomestays() async {
    setState(() => _isLoading = true);
    
    try {
      final homestays = await _homestayService.searchHomestays(city: widget.city);
      setState(() {
        _homestays = homestays;
        _createMarkers();
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải dữ liệu: $e')),
        );
      }
    }
  }

  void _createMarkers() {
    _markers = _homestays.map((homestay) {
      return Marker(
        markerId: MarkerId(homestay.id.toString()),
        position: LatLng(homestay.latitude, homestay.longitude),
        icon: BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueRed),
        infoWindow: InfoWindow(
          title: homestay.name,
          snippet: '${homestay.pricePerNight.toStringAsFixed(0)}₫/đêm',
          onTap: () => _showHomestayDetails(homestay),
        ),
        onTap: () {
          setState(() => _selectedHomestay = homestay);
          _mapController?.animateCamera(
            CameraUpdate.newLatLng(LatLng(homestay.latitude, homestay.longitude)),
          );
        },
      );
    }).toSet();
  }

  void _showHomestayDetails(Homestay homestay) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => HomestayDetailScreen(homestayId: homestay.id),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.city != null ? 'Bản đồ - ${widget.city}' : 'Bản đồ'),
        actions: [
          IconButton(
            icon: const Icon(Icons.my_location),
            onPressed: _getCurrentLocation,
          ),
        ],
      ),
      body: Stack(
        children: [
          GoogleMap(
            initialCameraPosition: CameraPosition(
              target: _initialPosition,
              zoom: 13,
            ),
            markers: _markers,
            onMapCreated: (controller) {
              _mapController = controller;
            },
            myLocationEnabled: true,
            myLocationButtonEnabled: false,
            zoomControlsEnabled: false,
          ),
          if (_isLoading)
            Container(
              color: Colors.black26,
              child: const Center(
                child: CircularProgressIndicator(color: Colors.white),
              ),
            ),
          if (_selectedHomestay != null)
            Positioned(
              bottom: 16,
              left: 16,
              right: 16,
              child: _buildHomestayCard(_selectedHomestay!),
            ),
        ],
      ),
    );
  }

  Widget _buildHomestayCard(Homestay homestay) {
    return Card(
      elevation: 8,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            if (homestay.images.isNotEmpty)
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.network(
                  homestay.images.first,
                  width: 80,
                  height: 80,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    width: 80,
                    height: 80,
                    color: Colors.grey[300],
                    child: const Icon(Icons.image_not_supported),
                  ),
                ),
              ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    homestay.name,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Icon(Icons.star, size: 16, color: Colors.amber[700]),
                      const SizedBox(width: 4),
                      Text('${homestay.averageRating?.toStringAsFixed(1) ?? "N/A"}'),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${homestay.pricePerNight.toStringAsFixed(0)}₫/đêm',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ],
              ),
            ),
            IconButton(
              icon: Icon(Icons.chevron_right, color: AppColors.primary),
              onPressed: () => _showHomestayDetails(homestay),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _mapController?.dispose();
    super.dispose();
  }
}
