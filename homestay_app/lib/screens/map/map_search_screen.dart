import 'dart:async';
import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../providers/homestay_provider.dart';

class MapSearchScreen extends StatefulWidget {
  const MapSearchScreen({Key? key}) : super(key: key);

  @override
  State<MapSearchScreen> createState() => _MapSearchScreenState();
}

class _MapSearchScreenState extends State<MapSearchScreen> {
  final Completer<GoogleMapController> _controller = Completer();
  final Set<Marker> _markers = {};
  Position? _currentPosition;
  bool _isLoading = true;
  Homestay? _selectedHomestay;

  static const CameraPosition _initialPosition = CameraPosition(
    target: LatLng(21.0285, 105.8542), // Hanoi, Vietnam
    zoom: 12,
  );

  @override
  void initState() {
    super.initState();
    _getCurrentLocation();
    // Use addPostFrameCallback to avoid setState during build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadHomestays();
    });
  }

  Future<void> _getCurrentLocation() async {
    try {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        setState(() => _isLoading = false);
        return;
      }

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          setState(() => _isLoading = false);
          return;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        setState(() => _isLoading = false);
        return;
      }

      final position = await Geolocator.getCurrentPosition();
      setState(() {
        _currentPosition = position;
      });

      final controller = await _controller.future;
      controller.animateCamera(
        CameraUpdate.newCameraPosition(
          CameraPosition(
            target: LatLng(position.latitude, position.longitude),
            zoom: 14,
          ),
        ),
      );
    } catch (e) {
      print('Error getting location: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadHomestays() async {
    final provider = Provider.of<HomestayProvider>(context, listen: false);
    await provider.searchHomestays();
    _updateMarkers(provider.homestays);
  }

  void _updateMarkers(List<Homestay> homestays) {
    final markers = <Marker>{};

    for (final homestay in homestays) {
      markers.add(
        Marker(
          markerId: MarkerId('homestay_${homestay.id}'),
          position: LatLng(homestay.latitude, homestay.longitude),
          infoWindow: InfoWindow(
            title: homestay.name,
            snippet: homestay.priceDisplay,
          ),
          icon: BitmapDescriptor.defaultMarkerWithHue(
            BitmapDescriptor.hueRed,
          ),
          onTap: () {
            setState(() {
              _selectedHomestay = homestay;
            });
          },
        ),
      );
    }

    setState(() {
      _markers.clear();
      _markers.addAll(markers);
    });
  }

  Future<void> _goToCurrentLocation() async {
    if (_currentPosition == null) {
      await _getCurrentLocation();
      return;
    }

    final controller = await _controller.future;
    controller.animateCamera(
      CameraUpdate.newCameraPosition(
        CameraPosition(
          target: LatLng(
            _currentPosition!.latitude,
            _currentPosition!.longitude,
          ),
          zoom: 15,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Tìm kiếm trên bản đồ'),
        backgroundColor: AppColors.primary,
        actions: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: () {
              // TODO: Open filter dialog
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Bộ lọc - Đang phát triển')),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadHomestays,
          ),
        ],
      ),
      body: Stack(
        children: [
          GoogleMap(
            mapType: MapType.normal,
            initialCameraPosition: _initialPosition,
            markers: _markers,
            myLocationEnabled: true,
            myLocationButtonEnabled: false,
            zoomControlsEnabled: false,
            onMapCreated: (GoogleMapController controller) {
              _controller.complete(controller);
            },
            onTap: (_) {
              setState(() {
                _selectedHomestay = null;
              });
            },
          ),
          if (_isLoading)
            Container(
              color: Colors.black26,
              child: const Center(
                child: CircularProgressIndicator(),
              ),
            ),
          Positioned(
            top: 16,
            left: 16,
            right: 16,
            child: _buildSearchBar(),
          ),
          Positioned(
            right: 16,
            bottom: _selectedHomestay != null ? 220 : 100,
            child: Column(
              children: [
                FloatingActionButton(
                  heroTag: 'location',
                  onPressed: _goToCurrentLocation,
                  backgroundColor: Colors.white,
                  child: const Icon(Icons.my_location, color: AppColors.primary),
                ),
                const SizedBox(height: 12),
                FloatingActionButton(
                  heroTag: 'list',
                  onPressed: () {
                    Navigator.pop(context);
                  },
                  backgroundColor: Colors.white,
                  child: const Icon(Icons.list, color: AppColors.primary),
                ),
              ],
            ),
          ),
          if (_selectedHomestay != null)
            Positioned(
              bottom: 16,
              left: 16,
              right: 16,
              child: _buildHomestayCard(_selectedHomestay!),
            ),
          Positioned(
            top: 80,
            left: 16,
            child: _buildLegend(),
          ),
        ],
      ),
    );
  }

  Widget _buildSearchBar() {
    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () {
          // TODO: Show search dialog
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Tìm kiếm - Đang phát triển')),
          );
        },
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              const Icon(Icons.search, color: AppColors.textSecondary),
              const SizedBox(width: 12),
              Expanded(
                child: Consumer<HomestayProvider>(
                  builder: (context, provider, child) {
                    return Text(
                      provider.searchCity ?? 'Tìm kiếm theo khu vực...',
                      style: TextStyle(
                        color: provider.searchCity != null
                            ? AppColors.textPrimary
                            : AppColors.textSecondary,
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLegend() {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
      ),
      child: Padding(
        padding: const EdgeInsets.all(8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  Icons.location_on,
                  color: Colors.red,
                  size: 20,
                ),
                const SizedBox(width: 6),
                Text(
                  '${_markers.length} homestay',
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHomestayCard(Homestay homestay) {
    return Card(
      elevation: 8,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      child: InkWell(
        onTap: () {
          Navigator.pushNamed(
            context,
            '/homestay-detail',
            arguments: homestay.id,
          );
        },
        borderRadius: BorderRadius.circular(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(16),
              ),
              child: homestay.images.isNotEmpty
                  ? Image.network(
                      homestay.images.first,
                      height: 120,
                      width: double.infinity,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) {
                        return Container(
                          height: 120,
                          color: Colors.grey[300],
                          child: const Icon(Icons.image_not_supported, size: 40),
                        );
                      },
                    )
                  : Container(
                      height: 120,
                      color: Colors.grey[300],
                      child: const Icon(Icons.home_work, size: 40),
                    ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    homestay.name,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Icon(
                        Icons.location_on,
                        size: 14,
                        color: Colors.grey[600],
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          homestay.address,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      if (homestay.averageRating != null)
                        Row(
                          children: [
                            const Icon(
                              Icons.star,
                              color: Colors.amber,
                              size: 16,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              homestay.averageRating!.toStringAsFixed(1),
                              style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                      Text(
                        homestay.priceDisplay,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              color: AppColors.primary,
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
