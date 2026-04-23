// Geocoding helper for Homestay forms
class GeocodingHelper {
    constructor() {
        this.apiBase = window.location.origin;
        this.lastRequestTime = 0;
        this.minRequestInterval = 1000; // 1 second rate limit
    }

    async makeAPIRequest(url) {
        const now = Date.now();
        if (now - this.lastRequestTime < this.minRequestInterval) {
            const waitTime = this.minRequestInterval - (now - this.lastRequestTime);
            await new Promise(resolve => setTimeout(resolve, waitTime));
        }
        this.lastRequestTime = Date.now();
        
        return await fetch(url);
    }

    async getCoordinatesFromAddress(address) {
        try {
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/geocoding/coordinates?address=${encodeURIComponent(address)}`
            );
            
            if (response.ok) {
                return await response.json();
            }
            return null;
        } catch (error) {
            console.error('Geocoding error:', error);
            return null;
        }
    }

    async getAddressFromCoordinates(latitude, longitude) {
        try {
            const response = await this.makeAPIRequest(
                `${this.apiBase}/api/geocoding/address?latitude=${latitude}&longitude=${longitude}`
            );
            
            if (response.ok) {
                return await response.json();
            }
            return null;
        } catch (error) {
            console.error('Reverse geocoding error:', error);
            return null;
        }
    }

    // Tự động điền tọa độ khi người dùng nhập địa chỉ
    initAddressGeocoding(addressFieldId, latFieldId, lngFieldId, triggerButtonId = null) {
        const addressField = document.getElementById(addressFieldId);
        const latField = document.getElementById(latFieldId);
        const lngField = document.getElementById(lngFieldId);
        
        if (!addressField || !latField || !lngField) {
            console.warn('Geocoding fields not found:', {addressFieldId, latFieldId, lngFieldId});
            return;
        }

        // Tạo button để trigger geocoding
        if (triggerButtonId) {
            const triggerButton = document.getElementById(triggerButtonId);
            if (triggerButton) {
                triggerButton.addEventListener('click', async () => {
                    await this.geocodeCurrentAddress(addressField, latField, lngField);
                });
            }
        }

        // Auto-geocoding khi blur khỏi address field
        addressField.addEventListener('blur', async () => {
            // Chỉ auto-geocode nếu chưa có tọa độ
            if ((!latField.value || latField.value === '0') && 
                (!lngField.value || lngField.value === '0') && 
                addressField.value.trim()) {
                await this.geocodeCurrentAddress(addressField, latField, lngField);
            }
        });
    }

    async geocodeCurrentAddress(addressField, latField, lngField) {
        const address = addressField.value.trim();
        if (!address) return;

        // Hiển thị trạng thái loading
        this.showGecodingStatus('🔍 Đang tìm tọa độ...', 'info');

        const result = await this.getCoordinatesFromAddress(address);
        
        if (result && result.latitude && result.longitude) {
            latField.value = result.latitude;
            lngField.value = result.longitude;
            this.showGecodingStatus('✅ Đã tìm thấy tọa độ!', 'success');
        } else {
            this.showGecodingStatus('❌ Không tìm thấy tọa độ cho địa chỉ này', 'error');
        }
    }

    // Hiển thị trạng thái geocoding
    showGecodingStatus(message, type = 'info') {
        // Tạo hoặc cập nhật status element
        let statusEl = document.getElementById('geocoding-status');
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.id = 'geocoding-status';
            statusEl.style.cssText = `
                padding: 8px 12px;
                margin: 5px 0;
                border-radius: 4px;
                font-size: 14px;
                transition: all 0.3s ease;
            `;
            
            // Thêm vào sau address field
            const addressField = document.querySelector('input[type="text"]');
            if (addressField && addressField.parentNode) {
                addressField.parentNode.insertBefore(statusEl, addressField.nextSibling);
            }
        }

        // Cập nhật style dựa trên type
        const styles = {
            info: 'background-color: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb;',
            success: 'background-color: #d4edda; color: #155724; border: 1px solid #c3e6cb;',
            error: 'background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb;'
        };

        statusEl.style.cssText += styles[type] || styles.info;
        statusEl.textContent = message;
        statusEl.style.display = 'block';

        // Tự động ẩn sau 3 giây
        setTimeout(() => {
            if (statusEl) {
                statusEl.style.display = 'none';
            }
        }, 3000);
    }

    // Lấy vị trí hiện tại của người dùng
    async getCurrentLocation() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation không được hỗ trợ'));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                position => resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                }),
                error => reject(error),
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 60000
                }
            );
        });
    }

    // Thêm button "Vị trí hiện tại" vào form
    addCurrentLocationButton(latFieldId, lngFieldId, containerId = null) {
        const latField = document.getElementById(latFieldId);
        const lngField = document.getElementById(lngFieldId);
        
        if (!latField || !lngField) return;

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-outline-primary btn-sm';
        button.innerHTML = '📍 Vị trí hiện tại';
        button.style.marginTop = '5px';

        button.addEventListener('click', async () => {
            button.disabled = true;
            button.innerHTML = '⏳ Đang lấy vị trí...';

            try {
                const location = await this.getCurrentLocation();
                latField.value = location.latitude;
                lngField.value = location.longitude;
                this.showGecodingStatus('✅ Đã lấy vị trí hiện tại!', 'success');
            } catch (error) {
                this.showGecodingStatus('❌ Không thể lấy vị trí: ' + error.message, 'error');
            } finally {
                button.disabled = false;
                button.innerHTML = '📍 Vị trí hiện tại';
            }
        });

        // Thêm button vào container hoặc sau lng field
        const container = containerId ? document.getElementById(containerId) : lngField.parentNode;
        if (container) {
            container.appendChild(button);
        }
    }

    // Khởi tạo toàn bộ geocoding cho form homestay
    initHomestayForm(config = {}) {
        const defaultConfig = {
            addressField: 'Address',
            latField: 'Latitude', 
            lngField: 'Longitude',
            geocodeButton: 'geocode-btn',
            addCurrentLocationBtn: true
        };

        const finalConfig = { ...defaultConfig, ...config };

        // Khởi tạo address geocoding
        this.initAddressGeocoding(
            finalConfig.addressField,
            finalConfig.latField,
            finalConfig.lngField,
            finalConfig.geocodeButton
        );

        // Thêm button vị trí hiện tại
        if (finalConfig.addCurrentLocationBtn) {
            this.addCurrentLocationButton(finalConfig.latField, finalConfig.lngField);
        }

        // Thêm geocoding button nếu chưa có
        if (finalConfig.geocodeButton && !document.getElementById(finalConfig.geocodeButton)) {
            this.addGeocodeButton(finalConfig.addressField, finalConfig.geocodeButton);
        }
    }

    // Thêm button geocode vào form
    addGeocodeButton(addressFieldId, buttonId) {
        const addressField = document.getElementById(addressFieldId);
        if (!addressField) return;

        const button = document.createElement('button');
        button.type = 'button';
        button.id = buttonId;
        button.className = 'btn btn-outline-secondary btn-sm';
        button.innerHTML = '🔍 Tìm tọa độ';
        button.style.marginTop = '5px';

        addressField.parentNode.appendChild(button);
    }
}

// Khởi tạo global instance
window.geocodingHelper = new GeocodingHelper();

// Auto-init khi DOM ready
document.addEventListener('DOMContentLoaded', () => {
    // Tự động khởi tạo cho form homestay nếu có các field cần thiết
    if (document.getElementById('Address') && 
        document.getElementById('Latitude') && 
        document.getElementById('Longitude')) {
        window.geocodingHelper.initHomestayForm();
    }
});
