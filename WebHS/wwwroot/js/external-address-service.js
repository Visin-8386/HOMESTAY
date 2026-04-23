/**
 * External Address API Service
 * Sử dụng API bên ngoài để lấy dữ liệu địa chỉ Việt Nam
 */

class ExternalAddressService {
    constructor() {
        // Sử dụng provinces.open-api.vn - API miễn phí và ổn định
        this.apiBase = 'https://provinces.open-api.vn/api';
        this.cache = new Map(); // Cache để giảm số lần gọi API
    }

    /**
     * Lấy danh sách tỉnh/thành phố
     */
    async getProvinces() {
        const cacheKey = 'provinces';
        
        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        try {
            const response = await fetch(`${this.apiBase}/p/`);
            const data = await response.json();
            
            // Chuyển đổi format để phù hợp với select box
            const provinces = data.map(province => ({
                id: province.code,
                name: province.name,
                code: province.code
            }));

            this.cache.set(cacheKey, provinces);
            return provinces;
        } catch (error) {
            console.error('Error loading provinces:', error);
            throw new Error('Không thể tải danh sách tỉnh/thành phố');
        }
    }

    /**
     * Lấy danh sách quận/huyện theo tỉnh
     * @param {string} provinceCode - Mã tỉnh (VD: "01" cho Hà Nội)
     */
    async getDistricts(provinceCode) {
        const cacheKey = `districts_${provinceCode}`;
        
        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        try {
            const response = await fetch(`${this.apiBase}/p/${provinceCode}?depth=2`);
            const data = await response.json();
            
            if (!data.districts) {
                return [];
            }

            // Chuyển đổi format
            const districts = data.districts.map(district => ({
                id: district.code,
                name: district.name,
                code: district.code,
                provinceCode: provinceCode
            }));

            this.cache.set(cacheKey, districts);
            return districts;
        } catch (error) {
            console.error('Error loading districts:', error);
            throw new Error('Không thể tải danh sách quận/huyện');
        }
    }

    /**
     * Lấy danh sách phường/xã theo quận
     * @param {string} districtCode - Mã quận (VD: "001" cho Ba Đình)
     */
    async getWards(districtCode) {
        const cacheKey = `wards_${districtCode}`;
        
        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        try {
            const response = await fetch(`${this.apiBase}/d/${districtCode}?depth=2`);
            const data = await response.json();
            
            if (!data.wards) {
                return [];
            }

            // Chuyển đổi format
            const wards = data.wards.map(ward => ({
                id: ward.code,
                name: ward.name,
                code: ward.code,
                districtCode: districtCode
            }));

            this.cache.set(cacheKey, wards);
            return wards;
        } catch (error) {
            console.error('Error loading wards:', error);
            throw new Error('Không thể tải danh sách phường/xã');
        }
    }

    /**
     * Tìm kiếm tỉnh theo tên
     * @param {string} searchTerm - Từ khóa tìm kiếm
     */
    async searchProvinces(searchTerm) {
        const provinces = await this.getProvinces();
        return provinces.filter(province => 
            province.name.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }

    /**
     * Tìm kiếm quận theo tên và tỉnh
     * @param {string} provinceCode - Mã tỉnh
     * @param {string} searchTerm - Từ khóa tìm kiếm
     */
    async searchDistricts(provinceCode, searchTerm) {
        const districts = await this.getDistricts(provinceCode);
        return districts.filter(district => 
            district.name.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }

    /**
     * Lấy thông tin đầy đủ của một địa chỉ
     * @param {string} provinceCode 
     * @param {string} districtCode 
     * @param {string} wardCode 
     */
    async getFullAddressInfo(provinceCode, districtCode, wardCode) {
        try {
            const [provinces, districts, wards] = await Promise.all([
                this.getProvinces(),
                this.getDistricts(provinceCode),
                this.getWards(districtCode)
            ]);

            const province = provinces.find(p => p.code === provinceCode);
            const district = districts.find(d => d.code === districtCode);
            const ward = wards.find(w => w.code === wardCode);

            return {
                province,
                district,
                ward,
                fullAddress: [ward?.name, district?.name, province?.name].filter(Boolean).join(', ')
            };
        } catch (error) {
            console.error('Error getting full address info:', error);
            throw new Error('Không thể lấy thông tin địa chỉ đầy đủ');
        }
    }
}

// Usage example:
/*
const addressService = new ExternalAddressService();

// Sử dụng trong form
async function initializeAddressForm() {
    try {
        // Load tỉnh/thành phố
        const provinces = await addressService.getProvinces();
        populateSelect('#provinceSelect', provinces);
        
        // Event handlers
        $('#provinceSelect').change(async function() {
            const provinceCode = $(this).val();
            if (provinceCode) {
                const districts = await addressService.getDistricts(provinceCode);
                populateSelect('#districtSelect', districts);
                resetSelect('#wardSelect');
            }
        });

        $('#districtSelect').change(async function() {
            const districtCode = $(this).val();
            if (districtCode) {
                const wards = await addressService.getWards(districtCode);
                populateSelect('#wardSelect', wards);
            }
        });
        
    } catch (error) {
        console.error('Failed to initialize address form:', error);
        alert('Không thể tải dữ liệu địa chỉ. Vui lòng thử lại sau.');
    }
}

function populateSelect(selector, options) {
    const select = $(selector);
    const placeholder = selector.includes('province') ? '-- Chọn tỉnh/thành phố --' :
                      selector.includes('district') ? '-- Chọn quận/huyện --' :
                      '-- Chọn phường/xã --';
    
    select.empty().append(`<option value="">${placeholder}</option>`);
    
    options.forEach(option => {
        select.append(`<option value="${option.code}">${option.name}</option>`);
    });
    
    select.prop('disabled', false);
}

function resetSelect(selector) {
    const select = $(selector);
    const placeholder = selector.includes('district') ? '-- Chọn quận/huyện --' : '-- Chọn phường/xã --';
    select.empty().append(`<option value="">${placeholder}</option>`).prop('disabled', true);
}
*/
