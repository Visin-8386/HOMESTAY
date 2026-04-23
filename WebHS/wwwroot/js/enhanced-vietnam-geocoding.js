/**
 * Enhanced Vietnam Geocoding Helper
 * Supports Google Maps + Nominatim + Local Database fallback
 */
class EnhancedVietnameseGeocodingHelper {
    constructor() {
        this.apiBase = window.location.origin;
        this.lastRequestTime = 0;
        this.minRequestInterval = 1000; // 1 second rate limit
        this.cache = new Map(); // Client-side cache
        this.preferredProvider = 'hybrid'; // hybrid, google, nominatim
    }

    async makeAPIRequest(url, options = {}) {
        const now = Date.now();
        if (now - this.lastRequestTime < this.minRequestInterval) {
            const waitTime = this.minRequestInterval - (now - this.lastRequestTime);
            await new Promise(resolve => setTimeout(resolve, waitTime));
        }
        this.lastRequestTime = Date.now();
        
        return await fetch(url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });
    }

    async getCoordinatesFromAddress(address) {
        const cacheKey = `coords_${address}`;
        if (this.cache.has(cacheKey)) {
            console.log('📦 Using cached coordinates for:', address);
            return this.cache.get(cacheKey);
        }

        try {
            console.log('🔍 Enhanced geocoding for:', address);
            
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/enhancedgeocoding/coordinates?address=${encodeURIComponent(address)}`
            );
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    this.cache.set(cacheKey, result);
                    console.log('✅ Enhanced geocoding successful:', result.source);
                    return result;
                }
            }
            return null;
        } catch (error) {
            console.error('❌ Enhanced geocoding error:', error);
            return null;
        }
    }

    async getAddressFromCoordinates(latitude, longitude) {
        const cacheKey = `addr_${latitude.toFixed(6)}_${longitude.toFixed(6)}`;
        if (this.cache.has(cacheKey)) {
            console.log('📦 Using cached address for coordinates');
            return this.cache.get(cacheKey);
        }

        try {
            console.log('🔍 Enhanced reverse geocoding for:', latitude, longitude);
            
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/enhancedgeocoding/address`, {
                method: 'POST',
                body: JSON.stringify({ latitude, longitude })
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    this.cache.set(cacheKey, result);
                    console.log('✅ Enhanced reverse geocoding successful:', result.source);
                    return result;
                }
            }
            return null;
        } catch (error) {
            console.error('❌ Enhanced reverse geocoding error:', error);
            return null;
        }
    }

    // Auto-fill form fields with Vietnamese address structure
    async autoFillVietnameseAddress(coordinates, addressComponents) {
        console.log('🏠 Auto-filling Vietnamese address structure...');
        
        try {
            // Map to Vietnamese address fields
            if (addressComponents.houseNumber) {
                const houseNumberField = document.getElementById('houseNumber') || 
                                       document.getElementById('HouseNumber');
                if (houseNumberField) {
                    houseNumberField.value = addressComponents.houseNumber;
                    this.highlightField(houseNumberField, 'success');
                }
            }

            if (addressComponents.streetName) {
                const streetField = document.getElementById('Address') || 
                                  document.getElementById('street') ||
                                  document.getElementById('StreetName');
                if (streetField) {
                    streetField.value = addressComponents.streetName;
                    this.highlightField(streetField, 'success');
                }
            }

            // Try to match with dropdown selections
            await this.matchAddressToDropdowns(addressComponents);

            // Show success notification
            this.showEnhancedNotification(`✅ Địa chỉ đã được điền tự động từ ${addressComponents.source || 'hệ thống geocoding'}`, 'success');

        } catch (error) {
            console.error('❌ Error auto-filling address:', error);
            this.showEnhancedNotification('⚠️ Có lỗi khi tự động điền địa chỉ', 'warning');
        }
    }

    async matchAddressToDropdowns(addressComponents) {
        try {
            // Match province/city
            if (addressComponents.province) {
                await this.selectDropdownByText('provinceSelect', addressComponents.province);
                await this.selectDropdownByText('citySelect', addressComponents.province);
            }

            // Match district
            if (addressComponents.district) {
                await new Promise(resolve => setTimeout(resolve, 500)); // Wait for province change
                await this.selectDropdownByText('districtSelect', addressComponents.district);
            }

            // Match ward
            if (addressComponents.ward) {
                await new Promise(resolve => setTimeout(resolve, 500)); // Wait for district change
                await this.selectDropdownByText('wardSelect', addressComponents.ward);
            }
        } catch (error) {
            console.error('❌ Error matching dropdowns:', error);
        }
    }

    async selectDropdownByText(selectId, searchText) {
        const select = document.getElementById(selectId);
        if (!select) return false;

        // Find option that contains the search text
        for (let option of select.options) {
            if (option.text.toLowerCase().includes(searchText.toLowerCase()) ||
                searchText.toLowerCase().includes(option.text.toLowerCase())) {
                select.value = option.value;
                select.dispatchEvent(new Event('change'));
                this.highlightField(select, 'success');
                console.log(`✅ Matched ${selectId}: ${option.text}`);
                return true;
            }
        }
        return false;
    }

    highlightField(field, type = 'success') {
        field.classList.remove('is-valid', 'is-invalid');
        if (type === 'success') {
            field.classList.add('is-valid');
            setTimeout(() => field.classList.remove('is-valid'), 3000);
        } else if (type === 'error') {
            field.classList.add('is-invalid');
            setTimeout(() => field.classList.remove('is-invalid'), 3000);
        }
    }

    showEnhancedNotification(message, type = 'info', duration = 5000) {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.enhanced-geocoding-notification');
        existingNotifications.forEach(n => n.remove());

        const notification = document.createElement('div');
        notification.className = `enhanced-geocoding-notification alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} alert-dismissible fade show`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            max-width: 500px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;

        notification.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'} me-2"></i>
                <span>${message}</span>
                <button type="button" class="btn-close ms-auto" aria-label="Close"></button>
            </div>
        `;

        // Add close functionality
        notification.querySelector('.btn-close').addEventListener('click', () => {
            notification.remove();
        });

        document.body.appendChild(notification);

        // Auto remove after duration
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, duration);
    }

    // Enhanced map integration
    async initializeEnhancedMap(mapElementId, options = {}) {
        const defaultOptions = {
            center: [21.0285, 105.8542], // Hanoi, Vietnam
            zoom: 13,
            enableAddressLookup: true,
            enableCurrentLocation: true,
            markerDraggable: true
        };

        const config = { ...defaultOptions, ...options };
        
        console.log('🗺️ Initializing enhanced Vietnam map...');

        const mapElement = document.getElementById(mapElementId);
        if (!mapElement) {
            console.error('❌ Map element not found:', mapElementId);
            return null;
        }

        // Initialize Leaflet map
        const map = L.map(mapElementId).setView(config.center, config.zoom);

        // Add Vietnam-optimized tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);

        let currentMarker = null;

        // Enhanced click handler
        map.on('click', async (e) => {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;

            console.log(`🎯 Map clicked: ${lat.toFixed(6)}, ${lng.toFixed(6)}`);

            // Add/update marker
            if (currentMarker) {
                map.removeLayer(currentMarker);
            }

            currentMarker = L.marker([lat, lng], {
                draggable: config.markerDraggable
            }).addTo(map);

            // Update coordinate inputs
            this.updateCoordinateInputs(lat, lng);

            // Enhanced reverse geocoding
            if (config.enableAddressLookup) {
                this.showEnhancedNotification('🔍 Đang tìm địa chỉ...', 'info', 2000);
                
                const addressResult = await this.getAddressFromCoordinates(lat, lng);
                if (addressResult && addressResult.success) {
                    await this.autoFillVietnameseAddress(
                        { lat, lng }, 
                        addressResult.components
                    );

                    // Update marker popup
                    const popupContent = this.createEnhancedPopupContent(addressResult, lat, lng);
                    currentMarker.bindPopup(popupContent).openPopup();
                } else {
                    this.showEnhancedNotification('⚠️ Không thể tìm địa chỉ cho vị trí này', 'warning');
                }
            }
        });

        // Enhanced marker drag handler
        if (config.markerDraggable) {
            map.on('layeradd', (e) => {
                if (e.layer instanceof L.Marker && e.layer.options.draggable) {
                    e.layer.on('dragend', async (dragEvent) => {
                        const newPos = dragEvent.target.getLatLng();
                        console.log(`🎯 Marker dragged to: ${newPos.lat.toFixed(6)}, ${newPos.lng.toFixed(6)}`);
                        
                        this.updateCoordinateInputs(newPos.lat, newPos.lng);
                        
                        if (config.enableAddressLookup) {
                            const addressResult = await this.getAddressFromCoordinates(newPos.lat, newPos.lng);
                            if (addressResult && addressResult.success) {
                                await this.autoFillVietnameseAddress(
                                    { lat: newPos.lat, lng: newPos.lng }, 
                                    addressResult.components
                                );
                            }
                        }
                    });
                }
            });
        }

        return {
            map,
            addMarker: (lat, lng) => {
                if (currentMarker) map.removeLayer(currentMarker);
                currentMarker = L.marker([lat, lng], { draggable: config.markerDraggable }).addTo(map);
                return currentMarker;
            },
            getCurrentMarker: () => currentMarker,
            setView: (lat, lng, zoom = config.zoom) => map.setView([lat, lng], zoom)
        };
    }

    updateCoordinateInputs(lat, lng) {
        const latInput = document.getElementById('Latitude') || document.getElementById('latitude');
        const lngInput = document.getElementById('Longitude') || document.getElementById('longitude');

        if (latInput) {
            latInput.value = lat.toFixed(6);
            this.highlightField(latInput, 'success');
        }
        if (lngInput) {
            lngInput.value = lng.toFixed(6);
            this.highlightField(lngInput, 'success');
        }
    }

    createEnhancedPopupContent(addressResult, lat, lng) {
        const components = addressResult.components;
        const source = addressResult.source || 'Unknown';
        
        return `
            <div class="enhanced-map-popup" style="min-width: 250px;">
                <h6 class="fw-bold text-primary mb-2">📍 Thông tin vị trí</h6>
                <div class="mb-2">
                    <strong>Địa chỉ:</strong><br>
                    <span class="text-muted">${addressResult.displayName || addressResult.address || 'Không xác định'}</span>
                </div>
                ${components.houseNumber ? `<div class="mb-1"><strong>Số nhà:</strong> ${components.houseNumber}</div>` : ''}
                ${components.streetName ? `<div class="mb-1"><strong>Đường:</strong> ${components.streetName}</div>` : ''}
                ${components.ward ? `<div class="mb-1"><strong>Phường/Xã:</strong> ${components.ward}</div>` : ''}
                ${components.district ? `<div class="mb-1"><strong>Quận/Huyện:</strong> ${components.district}</div>` : ''}
                ${components.province ? `<div class="mb-1"><strong>Tỉnh/TP:</strong> ${components.province}</div>` : ''}
                <div class="mb-2">
                    <strong>Tọa độ:</strong> ${lat.toFixed(6)}, ${lng.toFixed(6)}
                </div>
                <div class="mb-2">
                    <small class="text-info">📡 Nguồn: ${source}</small>
                </div>
                <div class="d-flex gap-1">
                    <a href="https://www.google.com/maps?q=${lat},${lng}" target="_blank" 
                       class="btn btn-sm btn-outline-primary">
                        🗺️ Google Maps
                    </a>
                </div>
            </div>
        `;
    }

    // Current location with Vietnamese address lookup
    async getCurrentLocationWithAddress() {
        try {
            this.showEnhancedNotification('📱 Đang lấy vị trí hiện tại...', 'info');
            
            const position = await new Promise((resolve, reject) => {
                if (!navigator.geolocation) {
                    reject(new Error('Geolocation không được hỗ trợ'));
                    return;
                }

                navigator.geolocation.getCurrentPosition(resolve, reject, {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 60000
                });
            });

            const lat = position.coords.latitude;
            const lng = position.coords.longitude;

            console.log('📱 Current location obtained:', lat, lng);

            // Update coordinates
            this.updateCoordinateInputs(lat, lng);

            // Get Vietnamese address
            const addressResult = await this.getAddressFromCoordinates(lat, lng);
            if (addressResult && addressResult.success) {
                await this.autoFillVietnameseAddress({ lat, lng }, addressResult.components);
                this.showEnhancedNotification('✅ Đã lấy vị trí và địa chỉ hiện tại', 'success');
            } else {
                this.showEnhancedNotification('⚠️ Đã lấy tọa độ nhưng không thể xác định địa chỉ', 'warning');
            }

            return { latitude: lat, longitude: lng, addressResult };

        } catch (error) {
            console.error('❌ Error getting current location:', error);
            this.showEnhancedNotification('❌ Không thể lấy vị trí hiện tại: ' + error.message, 'error');
            throw error;
        }
    }

    // Initialize enhanced form
    initializeEnhancedVietnameseForm(config = {}) {
        const defaultConfig = {
            mapElementId: 'map',
            enableAutoGeocode: true,
            enableCurrentLocation: true,
            enableMapIntegration: true,
            debounceTime: 1500
        };

        const finalConfig = { ...defaultConfig, ...config };

        console.log('🚀 Initializing enhanced Vietnamese geocoding form...');

        // Add enhanced map if enabled
        if (finalConfig.enableMapIntegration) {
            this.initializeEnhancedMap(finalConfig.mapElementId, {
                enableAddressLookup: true,
                enableCurrentLocation: finalConfig.enableCurrentLocation
            });
        }

        // Add current location button
        if (finalConfig.enableCurrentLocation) {
            this.addEnhancedCurrentLocationButton();
        }

        // Add geocoding button for address input
        if (finalConfig.enableAutoGeocode) {
            this.addEnhancedGeocodeButton(finalConfig.debounceTime);
        }

        this.showEnhancedNotification('🎯 Hệ thống geocoding Việt Nam đã sẵn sàng', 'success', 3000);
    }

    addEnhancedCurrentLocationButton() {
        const latField = document.getElementById('Latitude') || document.getElementById('latitude');
        if (!latField) return;

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-outline-success btn-sm mt-2';
        button.innerHTML = '📱 Vị trí hiện tại';
        
        button.addEventListener('click', async () => {
            button.disabled = true;
            button.innerHTML = '⏳ Đang lấy vị trí...';
            
            try {
                await this.getCurrentLocationWithAddress();
            } catch (error) {
                // Error already handled in getCurrentLocationWithAddress
            } finally {
                button.disabled = false;
                button.innerHTML = '📱 Vị trí hiện tại';
            }
        });

        latField.parentNode.appendChild(button);
    }

    addEnhancedGeocodeButton(debounceTime = 1500) {
        const addressField = document.getElementById('Address') || 
                            document.getElementById('address') ||
                            document.getElementById('fullAddress');
        
        if (!addressField) return;

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-outline-primary btn-sm mt-2';
        button.innerHTML = '🔍 Tìm trên bản đồ';
        
        button.addEventListener('click', async () => {
            const address = addressField.value.trim();
            if (!address) {
                this.showEnhancedNotification('⚠️ Vui lòng nhập địa chỉ', 'warning');
                return;
            }

            button.disabled = true;
            button.innerHTML = '⏳ Đang tìm...';
            
            try {
                const result = await this.getCoordinatesFromAddress(address);
                if (result && result.success) {
                    this.updateCoordinateInputs(result.latitude, result.longitude);
                    this.showEnhancedNotification(`✅ Đã tìm thấy tọa độ từ ${result.source}`, 'success');
                } else {
                    this.showEnhancedNotification('❌ Không tìm thấy tọa độ cho địa chỉ này', 'error');
                }
            } catch (error) {
                this.showEnhancedNotification('❌ Có lỗi khi tìm tọa độ', 'error');
            } finally {
                button.disabled = false;
                button.innerHTML = '🔍 Tìm trên bản đồ';
            }
        });

        addressField.parentNode.appendChild(button);

        // Add debounced auto-geocoding
        let debounceTimer;
        addressField.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(async () => {
                const address = addressField.value.trim();
                if (address.length > 10) {
                    console.log('🔄 Auto-geocoding for:', address);
                    // Optional: implement auto-geocoding here
                }
            }, debounceTime);
        });
    }
}

// Initialize global instance
window.enhancedVietnameseGeocoding = new EnhancedVietnameseGeocodingHelper();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Auto-initialize if map element exists
    if (document.getElementById('map')) {
        console.log('🎯 Auto-initializing enhanced Vietnamese geocoding...');
        window.enhancedVietnameseGeocoding.initializeEnhancedVietnameseForm();
    }
});
