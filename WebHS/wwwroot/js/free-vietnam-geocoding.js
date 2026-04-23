/**
 * Free Vietnam Geocoding Helper
 * Uses only free geocoding services - no API keys required
 * Perfect for users who cannot access Google Maps API
 */
class FreeVietnameseGeocodingHelper {
    constructor() {
        this.apiBase = window.location.origin;
        this.cache = new Map();
        this.requestQueue = [];
        this.processing = false;
        this.lastRequestTime = 0;
        this.minRequestInterval = 1000; // 1 second rate limit for free services
    }

    async makeAPIRequest(url, options = {}) {
        const now = Date.now();
        if (now - this.lastRequestTime < this.minRequestInterval) {
            const waitTime = this.minRequestInterval - (now - this.lastRequestTime);
            await new Promise(resolve => setTimeout(resolve, waitTime));
        }
        this.lastRequestTime = Date.now();
        
        const defaultOptions = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        };

        const requestOptions = { ...defaultOptions, ...options };
        
        if (requestOptions.body && typeof requestOptions.body === 'object') {
            requestOptions.body = JSON.stringify(requestOptions.body);
        }

        return await fetch(url, requestOptions);
    }

    async getCoordinatesFromAddress(address) {
        const cacheKey = `coords_${address}`;
        if (this.cache.has(cacheKey)) {
            console.log('📦 Using cached coordinates for:', address);
            return this.cache.get(cacheKey);
        }

        try {
            console.log('🔍 Free geocoding for:', address);
            
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/freegeocoding/coordinates?address=${encodeURIComponent(address)}`
            );
            
            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    this.cache.set(cacheKey, result);
                    console.log('✅ Free geocoding successful:', result.source);
                    return result;
                }
            }
            return null;
        } catch (error) {
            console.error('❌ Free geocoding error:', error);
            return null;
        }
    }

    // NEW: Smart geocoding with progressive fallback
    async getCoordinatesWithFallback(fullAddress) {
        console.log('🎯 Starting smart geocoding with fallback for:', fullAddress);
        
        // Try exact address first
        let result = await this.getCoordinatesFromAddress(fullAddress);
        if (result && result.success) {
            console.log('✅ Found exact address match');
            return { ...result, fallbackLevel: 0, searchedAddress: fullAddress };
        }

        // Parse address components for fallback
        const addressComponents = this.parseVietnameseAddress(fullAddress);
        console.log('📝 Parsed address components:', addressComponents);

        // Progressive fallback levels
        const fallbackQueries = this.generateFallbackQueries(addressComponents);
        console.log('🔄 Generated fallback queries:', fallbackQueries);

        for (let i = 0; i < fallbackQueries.length; i++) {
            const query = fallbackQueries[i];
            console.log(`🔍 Fallback level ${i + 1}: trying "${query}"`);
            
            result = await this.getCoordinatesFromAddress(query);
            if (result && result.success) {
                console.log(`✅ Found match at fallback level ${i + 1}`);
                return { 
                    ...result, 
                    fallbackLevel: i + 1, 
                    searchedAddress: query,
                    originalAddress: fullAddress,
                    fallbackType: this.getFallbackType(i + 1)
                };
            }
            
            // Wait between requests to avoid rate limiting
            await new Promise(resolve => setTimeout(resolve, 500));
        }

        console.log('❌ No matches found even with fallback');
        return null;
    }

    parseVietnameseAddress(address) {
        // Clean and normalize address
        const cleanAddress = address.trim().replace(/\s+/g, ' ');
        const parts = cleanAddress.split(',').map(p => p.trim()).filter(p => p);
        
        // Common Vietnamese address patterns
        const addressPattern = {
            houseNumber: '',
            streetName: '',
            ward: '',
            district: '',
            city: '',
            province: ''
        };

        if (parts.length >= 1) {
            // First part usually contains house number and street
            const firstPart = parts[0];
            const houseNumberMatch = firstPart.match(/^(\d+[A-Za-z]?(?:\/\d+)?)\s+(.+)$/);
            
            if (houseNumberMatch) {
                addressPattern.houseNumber = houseNumberMatch[1];
                addressPattern.streetName = houseNumberMatch[2];
            } else {
                addressPattern.streetName = firstPart;
            }
        }

        if (parts.length >= 2) addressPattern.ward = parts[1];
        if (parts.length >= 3) addressPattern.district = parts[2];
        if (parts.length >= 4) addressPattern.city = parts[3];
        if (parts.length >= 5) addressPattern.province = parts[4];

        return addressPattern;
    }

    generateFallbackQueries(components) {
        const queries = [];
        const { houseNumber, streetName, ward, district, city, province } = components;

        // Level 1: Street + Ward + District + City
        if (streetName && ward && district && city) {
            queries.push(`${streetName}, ${ward}, ${district}, ${city}`);
        }

        // Level 2: Street + District + City  
        if (streetName && district && city) {
            queries.push(`${streetName}, ${district}, ${city}`);
        }

        // Level 3: Ward + District + City
        if (ward && district && city) {
            queries.push(`${ward}, ${district}, ${city}`);
        }

        // Level 4: District + City
        if (district && city) {
            queries.push(`${district}, ${city}`);
        }

        // Level 5: City only
        if (city) {
            queries.push(city);
        }

        // Level 6: Province (if different from city)
        if (province && province !== city) {
            queries.push(province);
        }

        // Level 7: Try popular Vietnamese locations if nothing specific found
        const popularLocations = this.getPopularVietnameseLocations(components);
        queries.push(...popularLocations);

        return queries.filter((query, index, self) => self.indexOf(query) === index); // Remove duplicates
    }

    getPopularVietnameseLocations(components) {
        const locations = [];
        const { district, city } = components;

        // Common district/city combinations
        const popularAreas = [
            'Quận 1, TP.HCM',
            'Hoàn Kiếm, Hà Nội', 
            'Ba Đình, Hà Nội',
            'Nha Trang, Khánh Hòa',
            'Hội An, Quảng Nam',
            'Đà Lạt, Lâm Đồng',
            'Vũng Tàu, Bà Rịa - Vũng Tàu',
            'Phú Quốc, Kiên Giang'
        ];

        // Add popular areas that might match the components
        if (city) {
            const cityLower = city.toLowerCase();
            popularAreas.forEach(area => {
                if (area.toLowerCase().includes(cityLower)) {
                    locations.push(area);
                }
            });
        }

        if (district) {
            const districtLower = district.toLowerCase();
            popularAreas.forEach(area => {
                if (area.toLowerCase().includes(districtLower)) {
                    locations.push(area);
                }
            });
        }

        return locations.slice(0, 3); // Limit to 3 popular locations
    }

    getFallbackType(level) {
        const types = {
            1: 'Không có số nhà',
            2: 'Chỉ đường và quận/huyện', 
            3: 'Chỉ phường và quận/huyện',
            4: 'Chỉ quận/huyện và tỉnh/thành',
            5: 'Chỉ tỉnh/thành phố',
            6: 'Khu vực tỉnh',
            7: 'Địa điểm phổ biến gần nhất'
        };
        return types[level] || 'Tìm kiếm tổng quát';
    }

    async getAddressSuggestions(query, maxResults = 5) {
        if (!query || query.length < 2) return [];

        const cacheKey = `suggestions_${query}_${maxResults}`;
        if (this.cache.has(cacheKey)) {
            console.log('📦 Using cached suggestions for:', query);
            return this.cache.get(cacheKey);
        }

        try {
            console.log('🔍 Getting free geocoding suggestions for:', query);
            
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/freegeocoding/suggestions?query=${encodeURIComponent(query)}&maxResults=${maxResults}`
            );
            
            if (response.ok) {
                const result = await response.json();
                if (result.success && result.suggestions) {
                    this.cache.set(cacheKey, result.suggestions);
                    console.log(`✅ Found ${result.suggestions.length} free geocoding suggestions`);
                    return result.suggestions;
                }
            }
            return [];
        } catch (error) {
            console.error('❌ Free geocoding suggestions error:', error);
            return [];
        }
    }

    showNotification(message, type = 'info', duration = 3000) {
        // Create notification element
        const notification = document.createElement('div');
        const alertType = type === 'error' ? 'danger' : type === 'warning' ? 'warning' : type === 'success' ? 'success' : 'info';
        notification.className = `alert alert-${alertType} alert-dismissible fade show position-fixed`;
        notification.style.cssText = `
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            max-width: 400px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        `;
        
        const icon = type === 'success' ? '✅' : 
                    type === 'error' ? '❌' : 
                    type === 'warning' ? '⚠️' : 'ℹ️';
        
        notification.innerHTML = `
            <div class="d-flex align-items-start">
                <span class="me-2 mt-1">${icon}</span>
                <div class="flex-grow-1">${message}</div>
                <button type="button" class="btn-close ms-2" data-bs-dismiss="alert"></button>
            </div>
        `;

        document.body.appendChild(notification);

        // Auto remove after duration
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, duration);
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

    async autoFillVietnameseAddress(coordinates, components) {
        if (!components) return;

        const fields = {
            // Main address field
            'Address': components.houseNumber && components.streetName 
                ? `${components.houseNumber} ${components.streetName}` 
                : components.streetName || '',
            
            // Specific component fields  
            'HouseNumber': components.houseNumber || '',
            'StreetName': components.streetName || '',
            'Ward': components.ward || '',
            'District': components.district || '',
            'Province': components.province || '',
            'City': components.province || '',
            'Country': components.country || 'Việt Nam'
        };

        // Fill fields that exist in the form
        Object.entries(fields).forEach(([fieldName, value]) => {
            const field = document.getElementById(fieldName) || 
                         document.querySelector(`[name="${fieldName}"]`) ||
                         document.querySelector(`[data-field="${fieldName.toLowerCase()}"]`);
            
            if (field && value) {
                field.value = value;
                this.highlightField(field, 'success');
            }
        });

        this.showNotification('🎯 Đã tự động điền thông tin địa chỉ Việt Nam', 'success');
    }

    async initializeMap(mapElementId, options = {}) {
        const config = {
            zoom: 13,
            center: [10.8231, 106.6297], // Ho Chi Minh City default
            markerDraggable: true,
            enableAddressLookup: true,
            enableCurrentLocation: true,
            ...options
        };

        // Initialize Leaflet map
        const map = L.map(mapElementId).setView(config.center, config.zoom);
        
        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors | Free Geocoding Services'
        }).addTo(map);

        let currentMarker = null;

        // Map click handler
        map.on('click', async (e) => {
            const { lat, lng } = e.latlng;
            
            // Add/update marker
            if (currentMarker) map.removeLayer(currentMarker);
            currentMarker = L.marker([lat, lng], { draggable: config.markerDraggable }).addTo(map);

            // Update coordinate inputs
            this.updateCoordinateInputs(lat, lng);

            // Show loading popup
            currentMarker.bindPopup(`
                <div class="text-center">
                    <div class="spinner-border spinner-border-sm text-primary me-2"></div>
                    Đang tìm địa chỉ miễn phí...
                </div>
            `).openPopup();

            if (config.enableAddressLookup) {
                this.showNotification('🔍 Đang tìm địa chỉ bằng dịch vụ miễn phí...', 'info', 2000);
                
                // Free reverse geocoding would need to be implemented
                // For now, show coordinates
                const popupContent = this.createMapPopupContent(null, lat, lng);
                currentMarker.bindPopup(popupContent).openPopup();
            }

            // Draggable marker handler
            if (config.markerDraggable) {
                currentMarker.on('dragend', async (dragEvent) => {
                    const newPos = dragEvent.target.getLatLng();
                    this.updateCoordinateInputs(newPos.lat, newPos.lng);
                    
                    if (config.enableAddressLookup) {
                        // Free reverse geocoding lookup
                        const popupContent = this.createMapPopupContent(null, newPos.lat, newPos.lng);
                        currentMarker.bindPopup(popupContent).openPopup();
                    }
                });
            }
        });

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

    createMapPopupContent(addressResult, lat, lng) {
        const source = addressResult?.source || 'Manual Selection';
        
        return `
            <div class="free-map-popup" style="min-width: 250px;">
                <h6 class="fw-bold text-success mb-2">📍 Vị trí đã chọn (Miễn phí)</h6>
                <div class="mb-2">
                    <strong>Tọa độ:</strong> ${lat.toFixed(6)}, ${lng.toFixed(6)}
                </div>
                ${addressResult ? `
                    <div class="mb-2">
                        <strong>Địa chỉ:</strong><br>
                        <span class="text-muted">${addressResult.formattedAddress}</span>
                    </div>
                ` : ''}
                <div class="mb-2">
                    <small class="text-success">🌟 Nguồn: ${source}</small>
                </div>
                <div class="d-flex gap-1">
                    <a href="https://www.google.com/maps?q=${lat},${lng}" target="_blank" 
                       class="btn btn-sm btn-outline-success">
                        🗺️ Google Maps
                    </a>
                </div>
            </div>
        `;
    }

    // Current location with free services
    async getCurrentLocationWithAddress() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation is not supported by this browser'));
                return;
            }

            this.showNotification('📍 Đang lấy vị trí hiện tại...', 'info');

            navigator.geolocation.getCurrentPosition(
                async (position) => {
                    const { latitude, longitude } = position.coords;
                    
                    this.showNotification('✅ Đã lấy được vị trí hiện tại', 'success');
                    
                    resolve({
                        latitude,
                        longitude,
                        accuracy: position.coords.accuracy,
                        source: 'GPS/Network Location'
                    });
                },
                (error) => {
                    this.showNotification('❌ Không thể lấy vị trí hiện tại', 'error');
                    reject(error);
                },
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 300000
                }
            );
        });
    }

    // Initialize form with free geocoding features
    initializeFreeVietnameseForm(config = {}) {
        const defaultConfig = {
            enableGeocodeButton: false,
            enableCurrentLocationButton: false,
            enableAddressSuggestions: false,
            debounceTime: 1500,
            ...config
        };

        if (defaultConfig.enableGeocodeButton) {
            this.addFreeGeocodeButton(defaultConfig.debounceTime);
        }

        if (defaultConfig.enableCurrentLocationButton) {
            this.addFreeCurrentLocationButton();
        }

        if (defaultConfig.enableAddressSuggestions) {
            this.addFreeAddressSuggestions();
        }

        this.showNotification('🎉 Hệ thống địa chỉ Việt Nam miễn phí đã sẵn sàng!', 'success', 4000);
    }

    addFreeCurrentLocationButton() {
        const addressField = document.getElementById('Address') || document.querySelector('[name="Address"]');
        if (!addressField) return;

        const buttonContainer = document.createElement('div');
        buttonContainer.className = 'mt-2';
        
        const currentLocationBtn = document.createElement('button');
        currentLocationBtn.type = 'button';
        currentLocationBtn.className = 'btn btn-outline-success btn-sm';
        currentLocationBtn.innerHTML = '📍 Vị trí hiện tại (Miễn phí)';
        
        currentLocationBtn.addEventListener('click', async () => {
            try {
                currentLocationBtn.disabled = true;
                currentLocationBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang lấy vị trí...';
                
                const location = await this.getCurrentLocationWithAddress();
                
                // Update coordinates
                this.updateCoordinateInputs(location.latitude, location.longitude);
                
                this.showNotification('✅ Đã cập nhật tọa độ vị trí hiện tại', 'success');
                
            } catch (error) {
                console.error('Current location error:', error);
                this.showNotification('❌ Không thể lấy vị trí hiện tại', 'error');
            } finally {
                currentLocationBtn.disabled = false;
                currentLocationBtn.innerHTML = '📍 Vị trí hiện tại (Miễn phí)';
            }
        });

        buttonContainer.appendChild(currentLocationBtn);
        addressField.parentNode.appendChild(buttonContainer);
    }

    addFreeGeocodeButton(debounceTime = 1500) {
        const addressField = document.getElementById('Address') || document.querySelector('[name="Address"]');
        if (!addressField) return;

        let debounceTimeout;
        
        const geocodeButton = document.createElement('button');
        geocodeButton.type = 'button';
        geocodeButton.className = 'btn btn-primary btn-sm mt-2 me-2';
        geocodeButton.innerHTML = '🎯 Tìm tọa độ (Miễn phí)';
        
        geocodeButton.addEventListener('click', async () => {
            const address = addressField.value.trim();
            if (!address) {
                this.showNotification('⚠️ Vui lòng nhập địa chỉ', 'error');
                return;
            }

            try {
                geocodeButton.disabled = true;
                geocodeButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang tìm...';
                
                // Use smart geocoding with fallback
                const result = await this.getCoordinatesWithFallback(address);
                
                if (result && result.success) {
                    this.updateCoordinateInputs(result.latitude, result.longitude);
                    
                    if (result.components) {
                        await this.autoFillVietnameseAddress(
                            { lat: result.latitude, lng: result.longitude },
                            result.components
                        );
                    }
                    
                    // Show different messages based on fallback level
                    if (result.fallbackLevel === 0) {
                        this.showNotification(`✅ Đã tìm thấy địa chỉ chính xác (${result.source})`, 'success');
                    } else {
                        this.showNotification(
                            `⚠️ Tìm thấy khu vực gần nhất: ${result.fallbackType}<br>` +
                            `📍 Địa chỉ tìm được: ${result.searchedAddress}<br>` +
                            `💡 Vui lòng kiểm tra và điều chỉnh vị trí trên bản đồ`,
                            'warning',
                            6000
                        );
                    }
                } else {
                    this.showNotification(
                        '❌ Không tìm thấy địa chỉ nào.<br>' +
                        '💡 Vui lòng thử:<br>' +
                        '• Nhập ít chi tiết hơn (chỉ quận/huyện và tỉnh/thành)<br>' +
                        '• Kiểm tra chính tả<br>' +
                        '• Click trực tiếp trên bản đồ',
                        'error',
                        8000
                    );
                }
                
            } catch (error) {
                console.error('Geocoding error:', error);
                this.showNotification('❌ Lỗi khi tìm tọa độ', 'error');
            } finally {
                geocodeButton.disabled = false;
                geocodeButton.innerHTML = '🎯 Tìm tọa độ (Miễn phí)';
            }
        });

        // Auto-geocoding on address change with smart fallback
        addressField.addEventListener('input', () => {
            clearTimeout(debounceTimeout);
            debounceTimeout = setTimeout(async () => {
                const address = addressField.value.trim();
                if (address.length > 10) { // Only for substantial addresses
                    try {
                        // Try exact match first for auto-geocoding
                        let result = await this.getCoordinatesFromAddress(address);
                        if (result && result.success) {
                            this.updateCoordinateInputs(result.latitude, result.longitude);
                            this.showNotification(`🎯 Tự động tìm thấy địa chỉ chính xác (${result.source})`, 'info', 2000);
                        } else if (address.length > 20) {
                            // Try smart fallback for longer addresses
                            result = await this.getCoordinatesWithFallback(address);
                            if (result && result.success && result.fallbackLevel > 0) {
                                this.updateCoordinateInputs(result.latitude, result.longitude);
                                this.showNotification(
                                    `🔍 Tự động tìm thấy khu vực: ${result.fallbackType}`,
                                    'info',
                                    3000
                                );
                            }
                        }
                    } catch (error) {
                        // Ignore auto-geocoding errors
                    }
                }
            }, debounceTime);
        });

        addressField.parentNode.appendChild(geocodeButton);
    }

    addFreeAddressSuggestions() {
        const addressField = document.getElementById('Address') || document.querySelector('[name="Address"]');
        if (!addressField) return;

        let suggestionsContainer;
        let debounceTimeout;

        addressField.addEventListener('input', async () => {
            const query = addressField.value.trim();
            
            // Remove existing suggestions
            if (suggestionsContainer) {
                suggestionsContainer.remove();
                suggestionsContainer = null;
            }

            if (query.length < 2) return;

            clearTimeout(debounceTimeout);
            debounceTimeout = setTimeout(async () => {
                try {
                    const suggestions = await this.getAddressSuggestions(query, 5);
                    
                    if (suggestions.length > 0) {
                        this.showAddressSuggestions(addressField, suggestions);
                    }
                } catch (error) {
                    console.error('Address suggestions error:', error);
                }
            }, 800);
        });

        // Hide suggestions on click outside
        document.addEventListener('click', (e) => {
            if (suggestionsContainer && !suggestionsContainer.contains(e.target) && e.target !== addressField) {
                suggestionsContainer.remove();
                suggestionsContainer = null;
            }
        });
    }

    showAddressSuggestions(addressField, suggestions) {
        // Create suggestions container
        const suggestionsContainer = document.createElement('div');
        suggestionsContainer.className = 'position-absolute bg-white border rounded shadow-sm w-100';
        suggestionsContainer.style.cssText = `
            top: 100%;
            left: 0;
            z-index: 1000;
            max-height: 200px;
            overflow-y: auto;
        `;

        suggestions.forEach((suggestion, index) => {
            const suggestionItem = document.createElement('div');
            suggestionItem.className = 'px-3 py-2 border-bottom suggestion-item';
            suggestionItem.style.cursor = 'pointer';
            
            suggestionItem.innerHTML = `
                <div class="fw-bold text-primary">${suggestion.formattedAddress}</div>
                <small class="text-muted">📍 ${suggestion.source}</small>
            `;
            
            suggestionItem.addEventListener('mouseenter', () => {
                suggestionItem.classList.add('bg-light');
            });
            
            suggestionItem.addEventListener('mouseleave', () => {
                suggestionItem.classList.remove('bg-light');
            });
            
            suggestionItem.addEventListener('click', async () => {
                addressField.value = suggestion.formattedAddress;
                this.updateCoordinateInputs(suggestion.latitude, suggestion.longitude);
                
                if (suggestion.components) {
                    await this.autoFillVietnameseAddress(
                        { lat: suggestion.latitude, lng: suggestion.longitude },
                        suggestion.components
                    );
                }
                
                suggestionsContainer.remove();
                this.showNotification(`✅ Đã chọn địa chỉ (${suggestion.source})`, 'success');
            });
            
            suggestionsContainer.appendChild(suggestionItem);
        });

        // Position container relative to address field
        const fieldRect = addressField.getBoundingClientRect();
        addressField.parentNode.style.position = 'relative';
        addressField.parentNode.appendChild(suggestionsContainer);
    }
}

// Global instance
window.freeGeocodingHelper = new FreeVietnameseGeocodingHelper();

// Auto-initialize if DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        console.log('🌟 Free Vietnamese Geocoding Helper loaded successfully!');
    });
} else {
    console.log('🌟 Free Vietnamese Geocoding Helper loaded successfully!');
}
