import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:webview_flutter/webview_flutter.dart';

class MapPickerScreen extends StatefulWidget {
  final double initialLat;
  final double initialLng;

  const MapPickerScreen({super.key, required this.initialLat, required this.initialLng});

  @override
  // The state class is intentionally private; suppress the lint that warns about
  // returning a private type from a public API (this is standard for widgets).
  // ignore: library_private_types_in_public_api
  _MapPickerScreenState createState() => _MapPickerScreenState();
}

class _MapPickerScreenState extends State<MapPickerScreen> {
  late final WebViewController _controller;

  String _makeHtml(double lat, double lng) {
    final html = '''
    <!DOCTYPE html>
    <html>
    <head>
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
      <style>html,body,#map{height:100%;margin:0;padding:0;}</style>
    </head>
    <body>
      <div id="map"></div>
      <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
      <script>
        const initialLat = $lat;
        const initialLng = $lng;
    const map = L.map('map').setView([initialLat, initialLng], 15);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {maxZoom: 19}).addTo(map);
    const marker = L.marker([initialLat, initialLng], {draggable:true}).addTo(map);
  // Post the picked coordinates to the Flutter app via the JS channel 'Picker'.
  map.on('click', function(e){ marker.setLatLng(e.latlng); try { Picker.postMessage(JSON.stringify({lat: e.latlng.lat, lng: e.latlng.lng})); } catch (err) { /* fallback noop */ } });
        marker.on('dragend', function(e){ /* no-op */ });
        function getMarker() { const p = marker.getLatLng(); return JSON.stringify({lat: p.lat, lng: p.lng}); }
        window.getMarker = getMarker;
      </script>
    </body>
    </html>
    ''';
    return Uri.dataFromString(html, mimeType: 'text/html', encoding: Encoding.getByName('utf-8')).toString();
  }

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      // Add a JavaScript channel named 'Picker' which the page can post messages to.
      ..addJavaScriptChannel('Picker', onMessageReceived: (JavaScriptMessage msg) {
        try {
          final Map<String, dynamic> data = jsonDecode(msg.message) as Map<String, dynamic>;
          final lat = (data['lat'] as num?)?.toDouble();
          final lng = (data['lng'] as num?)?.toDouble();
          if (lat != null && lng != null) {
            // Avoid calling Navigator.pop while the navigator is locked by the WebView
            // or during another frame. Schedule the pop to run on the next microtask
            // so we don't hit the '!_debugLocked' assertion.
            if (mounted) {
              Future.microtask(() {
                if (!mounted) return;
                try {
                  Navigator.of(context).maybePop({'lat': lat, 'lng': lng});
                } catch (_) {
                  // swallow any navigation errors
                }
              });
            }
          }
        } catch (e) {
          // ignore malformed messages
        }
      })
      ..setNavigationDelegate(NavigationDelegate(
        onNavigationRequest: (NavigationRequest request) {
          // Allow normal navigation for tile loading etc. We no longer rely on
          // URL-based callbacks for picking, so nothing special here.
          return NavigationDecision.navigate;
        },
      ));
  }

  Future<Map<String, double>> _getMarker() async {
    final res = await _controller.runJavaScriptReturningResult('window.getMarker()');
    // result is a JS string like '"{"lat":21,...}"' or already JSON depending on platform
    String raw = res is String ? res : res.toString();
    // Clean possible surrounding quotes. Use raw strings to avoid unnecessary escapes.
    if (raw.startsWith(r'"') && raw.endsWith(r'"')) {
      // When the JS result is doubly-quoted and inner quotes are escaped (e.g. '"{...}"')
      raw = raw.substring(2, raw.length - 2).replaceAll(r'"', '"');
    } else if (raw.startsWith('"') && raw.endsWith('"')) {
      raw = raw.substring(1, raw.length - 1);
    }
    final Map<String, dynamic> obj = jsonDecode(raw);
    return {'lat': (obj['lat'] as num).toDouble(), 'lng': (obj['lng'] as num).toDouble()};
  }

  @override
  Widget build(BuildContext context) {
    final initialLat = widget.initialLat == 0.0 && widget.initialLng == 0.0 ? 21.0285 : widget.initialLat;
    final initialLng = widget.initialLat == 0.0 && widget.initialLng == 0.0 ? 105.8542 : widget.initialLng;
    final htmlUri = _makeHtml(initialLat, initialLng);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Pick location'),
        actions: [
          TextButton(
            onPressed: () async {
              try {
                final pos = await _getMarker();
                if (!mounted) return;
                Navigator.of(context).pop({'lat': pos['lat'], 'lng': pos['lng']});
              } catch (e) {
                if (!mounted) return;
                Navigator.of(context).pop(null);
              }
            },
            child: const Text('Save', style: TextStyle(color: Colors.white)),
          )
        ],
      ),
      body: WebViewWidget(controller: _controller..loadRequest(Uri.parse(htmlUri))),
    );
  }
}
