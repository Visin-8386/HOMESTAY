import 'dart:io';

import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:image_picker/image_picker.dart';

import '../../services/homestay_service.dart';
import '../../widgets/user_gradient_background.dart';
import 'map_picker_screen.dart';

class CreateHomestayScreen extends StatefulWidget {
  const CreateHomestayScreen({super.key});

  @override
  State<CreateHomestayScreen> createState() => _CreateHomestayScreenState();
}

class _CreateHomestayScreenState extends State<CreateHomestayScreen> {
  final _formKey = GlobalKey<FormState>();
  // storage not needed here; HomestayService will use ApiService for auth

  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _youtubeController = TextEditingController();
  final _addressController = TextEditingController();
  final _cityController = TextEditingController();
  final _stateController = TextEditingController();
  final _zipCodeController = TextEditingController();
  final _priceController = TextEditingController();
  final _maxGuestsController = TextEditingController();
  final _bedroomsController = TextEditingController();
  final _bathroomsController = TextEditingController();

  bool _isLoading = false;
  final HomestayService _homestayService = HomestayService();
  final ImagePicker _picker = ImagePicker();
  final List<File> _imageFiles = [];
  double _latitude = 0.0;
  double _longitude = 0.0;

  Future<void> _pickImages() async {
    try {
      final picked = await _picker.pickMultiImage();
      if (picked.isNotEmpty) {
        setState(() {
          _imageFiles.addAll(picked.map((x) => File(x.path)));
        });
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error picking images: $e')),
      );
    }
  }

  // Try to extract a YouTube video ID from a URL or return the input if it looks like an ID.
  String _extractYoutubeId(String input) {
    // common short url: youtu.be/ID
    final uri = Uri.tryParse(input);
    if (uri != null) {
      // youtu.be/<id>
      if (uri.host.contains('youtu.be')) {
        return uri.pathSegments.isNotEmpty ? uri.pathSegments.last : input;
      }
      // youtube.com watch?v=ID or /embed/ID
      if (uri.host.contains('youtube.com')) {
        if (uri.queryParameters.containsKey('v')) return uri.queryParameters['v']!;
        if (uri.pathSegments.contains('embed')) {
          final idx = uri.pathSegments.indexOf('embed');
          if (idx + 1 < uri.pathSegments.length) return uri.pathSegments[idx + 1];
        }
      }
    }
    // fallback: return last 11 chars if looks like id length
    if (input.length >= 11) return input.substring(input.length - 11);
    return input;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _youtubeController.dispose();
    _addressController.dispose();
    _cityController.dispose();
    _stateController.dispose();
    _zipCodeController.dispose();
    _priceController.dispose();
    _maxGuestsController.dispose();
    _bedroomsController.dispose();
    _bathroomsController.dispose();
    super.dispose();
  }

  Future<void> _createHomestay() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isLoading = true;
    });

    try {
      final data = {
        'name': _nameController.text,
        'description': _descriptionController.text,
        // youtubeVideoId is optional; backend may accept null/absent
        if (_youtubeController.text.trim().isNotEmpty) 'youtubeVideoId': _extractYoutubeId(_youtubeController.text.trim()),
        'address': _addressController.text,
        'city': _cityController.text,
        'state': _stateController.text,
        'zipCode': _zipCodeController.text,
        'latitude': _latitude,
        'longitude': _longitude,
        'pricePerNight': double.parse(_priceController.text),
        'maxGuests': int.parse(_maxGuestsController.text),
        'bedrooms': int.parse(_bedroomsController.text),
        'bathrooms': int.parse(_bathroomsController.text),
        'amenityIds': [],
      };

      if (_imageFiles.isNotEmpty) {
        // send multipart via HomestayService (it expects file paths as 'imagePaths')
        final imagePaths = _imageFiles.map((f) => f.path).toList();
        data['imagePaths'] = imagePaths;
        await _homestayService.createHomestay(data);
      } else {
        await _homestayService.createHomestay(data);
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Homestay created successfully')),
      );
      Navigator.of(context).pop();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error creating homestay: $e')),
      );
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        title: const Text('Create Homestay'),
        actions: [
          if (_isLoading)
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            TextButton(
              onPressed: _createHomestay,
              child: const Text('Create', style: TextStyle(color: Colors.white)),
            ),
        ],
      ),
      body: UserGradientBackground(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Form(
            key: _formKey,
            child: Column(
              children: <Widget>[
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton.icon(
                    onPressed: _pickImages,
                    icon: const Icon(Icons.add_a_photo),
                    label: const Text('Add images'),
                  ),
                ),
                if (_imageFiles.isNotEmpty)
                  SizedBox(
                    height: 120,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      children: _imageFiles.map((f) {
                        return Container(
                          width: 120,
                          margin: const EdgeInsets.only(right: 8),
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(8),
                            image: DecorationImage(image: FileImage(f), fit: BoxFit.cover),
                          ),
                        );
                      }).toList(),
                    ),
                  ),
                const SizedBox(height: 12),
                Row(
                    children: [
                      Expanded(
                        child: Text('Location: ${_latitude.toStringAsFixed(4)}, ${_longitude.toStringAsFixed(4)}'),
                      ),
                      TextButton(
                        onPressed: () async {
                          final result = await Navigator.of(context).push<dynamic>(
                            MaterialPageRoute(builder: (_) => MapPickerScreen(initialLat: _latitude, initialLng: _longitude)),
                          );
                        if (result is Map) {
                          final lat = (result['lat'] is num) ? (result['lat'] as num).toDouble() : null;
                          final lng = (result['lng'] is num) ? (result['lng'] as num).toDouble() : null;
                          if (lat != null && lng != null) {
                            setState(() {
                              _latitude = lat;
                              _longitude = lng;
                            });
                          }
                        } else if (result is LatLng) {
                          setState(() {
                            _latitude = result.latitude;
                            _longitude = result.longitude;
                          });
                        }
                      },
                      child: const Text('Pick on map'),
                    )
                  ],
                ),
                TextFormField(
                  controller: _nameController,
                  decoration: const InputDecoration(labelText: 'Name'),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter homestay name';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _descriptionController,
                  decoration: const InputDecoration(labelText: 'Description'),
                  maxLines: 3,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter description';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _youtubeController,
                  decoration: const InputDecoration(
                    labelText: 'YouTube video (ID or URL)',
                    hintText: 'e.g. dQw4w9WgXcQ or https://youtu.be/dQw4w9WgXcQ',
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return null;
                    final v = value.trim();
                    if (v.length < 11 && !v.contains('youtube')) {
                      return 'Enter a valid YouTube ID or URL';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _addressController,
                  decoration: const InputDecoration(labelText: 'Address'),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter address';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _cityController,
                  decoration: const InputDecoration(labelText: 'City'),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter city';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _stateController,
                  decoration: const InputDecoration(labelText: 'State'),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter state';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _zipCodeController,
                  decoration: const InputDecoration(labelText: 'Zip Code'),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter zip code';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _priceController,
                  decoration: const InputDecoration(labelText: 'Price per Night'),
                  keyboardType: TextInputType.number,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter price';
                    }
                    if (double.tryParse(value) == null) {
                      return 'Please enter valid price';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _maxGuestsController,
                  decoration: const InputDecoration(labelText: 'Max Guests'),
                  keyboardType: TextInputType.number,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter max guests';
                    }
                    if (int.tryParse(value) == null) {
                      return 'Please enter valid number';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _bedroomsController,
                  decoration: const InputDecoration(labelText: 'Bedrooms'),
                  keyboardType: TextInputType.number,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter bedrooms';
                    }
                    if (int.tryParse(value) == null) {
                      return 'Please enter valid number';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _bathroomsController,
                  decoration: const InputDecoration(labelText: 'Bathrooms'),
                  keyboardType: TextInputType.number,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please enter bathrooms';
                    }
                    if (int.tryParse(value) == null) {
                      return 'Please enter valid number';
                    }
                    return null;
                  },
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}