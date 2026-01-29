/**
 * AddressHandler - Handles Vietnam address cascading dropdowns with caching
 * 
 * Features:
 * - Cascading dropdowns for Province → District → Ward
 * - LocalStorage caching with 24-hour TTL
 * - Diacritics-insensitive search
 * - Auto-restore saved addresses
 * - Searchable dropdowns with custom search UI
 * 
 * Requirements covered:
 * - 1.1, 2.1, 3.1: Display dropdowns for Province, District, Ward
 * - 1.2: Sort provinces alphabetically
 * - 2.2, 2.4: Load districts by province with loading indicator
 * - 2.3, 3.3: Reset cascading dropdowns
 * - 3.2, 3.4: Load wards by district with loading indicator
 * - 6.1: Cache provinces with 24-hour TTL
 * - 6.2: Cache districts by province_code with 24-hour TTL
 * - 6.3: Auto-fetch when cache expires
 * - 6.4: Use cache instead of API when available
 * - 7.1, 7.2, 7.3: Search/filter with diacritics support
 * - 8.1, 8.2: Restore saved addresses
 */
class AddressHandler {
    /**
     * Default cache TTL in hours
     * @type {number}
     */
    static DEFAULT_TTL_HOURS = 24;

    /**
     * Cache key prefix for localStorage
     * @type {string}
     */
    static CACHE_PREFIX = 'address_cache_';

    /**
     * Create an AddressHandler instance
     * 
     * **Validates: Requirements 1.1, 2.1, 3.1**
     * 
     * @param {Object} options - Configuration options
     * @param {string} options.provinceSelector - CSS selector for province dropdown
     * @param {string} options.districtSelector - CSS selector for district dropdown
     * @param {string} options.wardSelector - CSS selector for ward dropdown
     * @param {string} options.streetAddressSelector - CSS selector for street address input
     * @param {string} [options.provinceNameSelector] - CSS selector for province name hidden input
     * @param {string} [options.districtNameSelector] - CSS selector for district name hidden input
     * @param {string} [options.wardNameSelector] - CSS selector for ward name hidden input
     * @param {string} [options.apiBaseUrl='/api/address'] - Base URL for address API
     * @param {boolean} [options.enableSearch=true] - Enable search functionality in dropdowns
     * @param {Function} [options.onProvinceChange] - Callback when province changes
     * @param {Function} [options.onDistrictChange] - Callback when district changes
     * @param {Function} [options.onWardChange] - Callback when ward changes
     * @param {Function} [options.onError] - Callback when error occurs
     * @param {Function} [options.onLoad] - Callback when initial load completes
     */
    constructor(options) {
        this.options = {
            apiBaseUrl: '/api/address',
            enableSearch: true,
            ...options
        };

        this.provinceSelect = document.querySelector(this.options.provinceSelector);
        this.districtSelect = document.querySelector(this.options.districtSelector);
        this.wardSelect = document.querySelector(this.options.wardSelector);
        this.streetAddressInput = document.querySelector(this.options.streetAddressSelector);
        
        // Hidden inputs for names
        this.provinceNameInput = this.options.provinceNameSelector ? 
            document.querySelector(this.options.provinceNameSelector) : null;
        this.districtNameInput = this.options.districtNameSelector ? 
            document.querySelector(this.options.districtNameSelector) : null;
        this.wardNameInput = this.options.wardNameSelector ? 
            document.querySelector(this.options.wardNameSelector) : null;

        // Store data for search
        this._provincesData = [];
        this._districtsData = [];
        this._wardsData = [];

        // Search wrappers
        this._searchWrappers = new Map();

        // Bind event handlers
        this._bindEvents();
        
        // Initialize search if enabled
        if (this.options.enableSearch) {
            this._initializeSearch();
        }
    }

    // ==================== CACHING METHODS ====================

    /**
     * Get data from localStorage cache if exists and not expired
     * Returns null if cache miss or expired
     * 
     * **Validates: Requirements 6.4**
     * 
     * @param {string} key - Cache key
     * @returns {*} Cached data or null if not found/expired
     */
    getFromCache(key) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;
        
        try {
            const cached = localStorage.getItem(cacheKey);
            if (!cached) {
                return null;
            }

            const entry = JSON.parse(cached);
            const now = Date.now();

            // Check if cache has expired
            if (now > entry.expiresAt) {
                // Remove expired entry
                localStorage.removeItem(cacheKey);
                return null;
            }

            return entry.data;
        } catch (error) {
            console.warn('Error reading from cache:', error);
            return null;
        }
    }

    /**
     * Store data in localStorage cache with TTL
     * Default TTL is 24 hours
     * 
     * **Validates: Requirements 6.1, 6.2**
     * 
     * @param {string} key - Cache key
     * @param {*} data - Data to cache
     * @param {number} [ttlHours=24] - Time to live in hours
     */
    setToCache(key, data, ttlHours = AddressHandler.DEFAULT_TTL_HOURS) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;
        const now = Date.now();
        const ttlMs = ttlHours * 60 * 60 * 1000; // Convert hours to milliseconds

        const entry = {
            data: data,
            storedAt: now,
            expiresAt: now + ttlMs
        };

        try {
            localStorage.setItem(cacheKey, JSON.stringify(entry));
        } catch (error) {
            console.warn('Error writing to cache:', error);
            // If localStorage is full, try to clear old entries
            this._clearExpiredCache();
            try {
                localStorage.setItem(cacheKey, JSON.stringify(entry));
            } catch (retryError) {
                console.error('Failed to write to cache after cleanup:', retryError);
            }
        }
    }

    /**
     * Check if cache entry exists and is not expired
     * 
     * @param {string} key - Cache key
     * @returns {boolean} True if valid cache exists
     */
    hasValidCache(key) {
        return this.getFromCache(key) !== null;
    }

    /**
     * Invalidate (remove) a cache entry
     * 
     * @param {string} key - Cache key to invalidate
     */
    invalidateCache(key) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;
        try {
            localStorage.removeItem(cacheKey);
        } catch (error) {
            console.warn('Error invalidating cache:', error);
        }
    }

    /**
     * Clear all address cache entries
     */
    clearAllCache() {
        try {
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(AddressHandler.CACHE_PREFIX)) {
                    keysToRemove.push(key);
                }
            }
            keysToRemove.forEach(key => localStorage.removeItem(key));
        } catch (error) {
            console.warn('Error clearing cache:', error);
        }
    }

    /**
     * Clear expired cache entries
     * @private
     */
    _clearExpiredCache() {
        try {
            const now = Date.now();
            const keysToRemove = [];
            
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(AddressHandler.CACHE_PREFIX)) {
                    try {
                        const cached = localStorage.getItem(key);
                        if (cached) {
                            const entry = JSON.parse(cached);
                            if (now > entry.expiresAt) {
                                keysToRemove.push(key);
                            }
                        }
                    } catch (e) {
                        // Invalid entry, remove it
                        keysToRemove.push(key);
                    }
                }
            }
            
            keysToRemove.forEach(key => localStorage.removeItem(key));
        } catch (error) {
            console.warn('Error clearing expired cache:', error);
        }
    }


    // ==================== API METHODS WITH CACHING ====================

    /**
     * Load provinces from API or cache
     * Auto-fetches from API if cache is expired
     * 
     * **Validates: Requirements 1.1, 1.2, 6.3, 6.4**
     * 
     * @returns {Promise<Array>} List of provinces
     */
    async loadProvinces() {
        const cacheKey = 'provinces';
        
        // Check cache first (Requirement 6.4)
        const cached = this.getFromCache(cacheKey);
        if (cached) {
            this._provincesData = cached;
            this._populateProvinceDropdown(cached);
            this._enableDropdown(this.provinceSelect);
            return cached;
        }

        // Cache miss or expired - fetch from API (Requirement 6.3)
        try {
            this._showLoading(this.provinceSelect);
            
            // Use AbortController for timeout (10 seconds)
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 10000);
            
            const response = await fetch(`${this.options.apiBaseUrl}/provinces`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const provinces = await response.json();
            
            // Store in cache with 24-hour TTL (Requirement 6.1)
            this.setToCache(cacheKey, provinces);
            
            this._provincesData = provinces;
            this._populateProvinceDropdown(provinces);
            this._enableDropdown(this.provinceSelect);
            return provinces;
        } catch (error) {
            this._handleError('Không thể tải danh sách tỉnh/thành phố', error);
            this._enableManualInput();
            return [];
        } finally {
            this._hideLoading(this.provinceSelect);
        }
    }

    /**
     * Load districts by province code from API or cache
     * Auto-fetches from API if cache is expired
     * 
     * **Validates: Requirements 2.2, 2.4, 6.2, 6.3, 6.4**
     * 
     * @param {number} provinceCode - Province code
     * @returns {Promise<Array>} List of districts
     */
    async loadDistricts(provinceCode) {
        if (!provinceCode) {
            this._resetDropdown(this.districtSelect);
            this._resetDropdown(this.wardSelect);
            this._disableDropdown(this.districtSelect);
            this._disableDropdown(this.wardSelect);
            return [];
        }

        const cacheKey = `districts_${provinceCode}`;
        
        // Check cache first (Requirement 6.4)
        const cached = this.getFromCache(cacheKey);
        if (cached) {
            this._districtsData = cached;
            this._populateDistrictDropdown(cached);
            this._enableDropdown(this.districtSelect);
            return cached;
        }

        // Cache miss or expired - fetch from API (Requirement 6.3)
        try {
            // Show loading indicator (Requirement 2.4)
            this._showLoading(this.districtSelect);
            
            // Use AbortController for timeout (10 seconds)
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 10000);
            
            const response = await fetch(`${this.options.apiBaseUrl}/districts/${provinceCode}`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const districts = await response.json();
            
            // Handle empty results (Requirement 2.5)
            if (!districts || districts.length === 0) {
                this._showNoData(this.districtSelect, 'Không có dữ liệu');
                return [];
            }
            
            // Store in cache with 24-hour TTL by province_code (Requirement 6.2)
            this.setToCache(cacheKey, districts);
            
            this._districtsData = districts;
            this._populateDistrictDropdown(districts);
            this._enableDropdown(this.districtSelect);
            return districts;
        } catch (error) {
            this._handleError('Không thể tải danh sách quận/huyện', error);
            return [];
        } finally {
            this._hideLoading(this.districtSelect);
        }
    }

    /**
     * Load wards by district code from API or cache
     * Auto-fetches from API if cache is expired
     * 
     * **Validates: Requirements 3.2, 3.4, 6.3, 6.4**
     * 
     * @param {number} districtCode - District code
     * @returns {Promise<Array>} List of wards
     */
    async loadWards(districtCode) {
        if (!districtCode) {
            this._resetDropdown(this.wardSelect);
            this._disableDropdown(this.wardSelect);
            return [];
        }

        const cacheKey = `wards_${districtCode}`;
        
        // Check cache first (Requirement 6.4)
        const cached = this.getFromCache(cacheKey);
        if (cached) {
            this._wardsData = cached;
            this._populateWardDropdown(cached);
            this._enableDropdown(this.wardSelect);
            return cached;
        }

        // Cache miss or expired - fetch from API (Requirement 6.3)
        try {
            // Show loading indicator (Requirement 3.4)
            this._showLoading(this.wardSelect);
            
            // Use AbortController for timeout (10 seconds)
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 10000);
            
            const response = await fetch(`${this.options.apiBaseUrl}/wards/${districtCode}`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const wards = await response.json();
            
            // Store in cache with 24-hour TTL
            this.setToCache(cacheKey, wards);
            
            this._wardsData = wards;
            this._populateWardDropdown(wards);
            this._enableDropdown(this.wardSelect);
            return wards;
        } catch (error) {
            this._handleError('Không thể tải danh sách phường/xã', error);
            return [];
        } finally {
            this._hideLoading(this.wardSelect);
        }
    }

    // ==================== SEARCH/FILTER METHODS ====================

    /**
     * Initialize search functionality for dropdowns
     * Creates searchable wrapper around each dropdown
     * 
     * **Validates: Requirements 7.1, 7.2, 7.3**
     * 
     * @private
     */
    _initializeSearch() {
        [this.provinceSelect, this.districtSelect, this.wardSelect].forEach(select => {
            if (select) {
                this._createSearchWrapper(select);
            }
        });
    }

    /**
     * Create a searchable wrapper around a select element
     * Implements custom search UI with diacritics-insensitive filtering
     * 
     * **Validates: Requirements 7.1, 7.2, 7.3**
     * 
     * @private
     * @param {HTMLSelectElement} select - Select element to wrap
     */
    _createSearchWrapper(select) {
        // Create wrapper container
        const wrapper = document.createElement('div');
        wrapper.className = 'address-search-wrapper';
        wrapper.style.cssText = 'position: relative;';

        // Create search input with improved styling
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'address-search-input form-control';
        searchInput.placeholder = 'Gõ để tìm kiếm...';
        searchInput.autocomplete = 'off';
        searchInput.style.cssText = `
            display: none;
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            z-index: 1000;
            border-radius: 0;
            border-top: none;
            border-color: #81c408;
            box-shadow: none;
            padding: 8px 12px;
            font-size: 14px;
        `;

        // Create search icon indicator
        const searchIcon = document.createElement('span');
        searchIcon.className = 'address-search-icon';
        searchIcon.innerHTML = '<i class="fa fa-search"></i>';
        searchIcon.style.cssText = `
            display: none;
            position: absolute;
            right: 12px;
            top: 50%;
            transform: translateY(-50%);
            color: #81c408;
            pointer-events: none;
            z-index: 5;
        `;

        // Create dropdown list with improved styling
        const dropdownList = document.createElement('div');
        dropdownList.className = 'address-search-dropdown';
        dropdownList.style.cssText = `
            display: none;
            position: absolute;
            top: calc(100% + 38px);
            left: 0;
            right: 0;
            max-height: 250px;
            overflow-y: auto;
            background: white;
            border: 1px solid #81c408;
            border-top: none;
            border-radius: 0 0 6px 6px;
            z-index: 1001;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;

        // Insert wrapper
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        wrapper.appendChild(searchIcon);
        wrapper.appendChild(searchInput);
        wrapper.appendChild(dropdownList);

        // Store references
        this._searchWrappers.set(select, { wrapper, searchInput, dropdownList, searchIcon });

        // Bind search events
        this._bindSearchEvents(select, searchInput, dropdownList);
        
        // Add visual indicator that dropdown is searchable
        this._addSearchIndicator(select);
    }

    /**
     * Add visual indicator to show dropdown is searchable
     * 
     * @private
     * @param {HTMLSelectElement} select - Select element
     */
    _addSearchIndicator(select) {
        // Add a subtle visual cue that the dropdown is searchable
        select.title = 'Nhấp để tìm kiếm';
        select.style.cursor = 'pointer';
    }

    /**
     * Bind search events for a dropdown
     * Handles keyboard navigation and mouse interactions
     * 
     * **Validates: Requirements 7.1, 7.2, 7.3**
     * 
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {HTMLInputElement} searchInput - Search input element
     * @param {HTMLElement} dropdownList - Dropdown list element
     */
    _bindSearchEvents(select, searchInput, dropdownList) {
        let isOpen = false;

        // Prevent native dropdown from opening
        select.addEventListener('mousedown', (e) => {
            if (!select.disabled && select.options.length > 1) {
                e.preventDefault();
                e.stopPropagation();
            }
        });

        // Show search on select focus/click
        select.addEventListener('focus', () => {
            if (!select.disabled && select.options.length > 1) {
                this._showSearchDropdown(select, searchInput, dropdownList);
                isOpen = true;
            }
        });

        select.addEventListener('click', (e) => {
            if (!select.disabled && select.options.length > 1) {
                e.preventDefault();
                e.stopPropagation();
                if (!isOpen) {
                    this._showSearchDropdown(select, searchInput, dropdownList);
                    isOpen = true;
                }
            }
        });

        // Handle search input with debounce for better performance
        let searchTimeout;
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                this._filterDropdownOptions(select, searchInput.value, dropdownList);
            }, 100);
        });

        // Handle keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this._hideSearchDropdown(searchInput, dropdownList, select);
                select.focus();
                isOpen = false;
            } else if (e.key === 'Enter') {
                e.preventDefault();
                const firstItem = dropdownList.querySelector('.address-search-item:not(.no-results)');
                if (firstItem) {
                    firstItem.click();
                }
            } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                const items = dropdownList.querySelectorAll('.address-search-item:not(.no-results)');
                if (items.length > 0) {
                    items[0].focus();
                }
            } else if (e.key === 'Tab') {
                this._hideSearchDropdown(searchInput, dropdownList, select);
                isOpen = false;
            }
        });

        // Close on click outside
        document.addEventListener('click', (e) => {
            const wrapper = this._searchWrappers.get(select)?.wrapper;
            if (wrapper && !wrapper.contains(e.target)) {
                this._hideSearchDropdown(searchInput, dropdownList, select);
                isOpen = false;
            }
        });
    }

    /**
     * Show search dropdown with options
     * 
     * **Validates: Requirements 7.1, 7.2, 7.3**
     * 
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {HTMLInputElement} searchInput - Search input element
     * @param {HTMLElement} dropdownList - Dropdown list element
     */
    _showSearchDropdown(select, searchInput, dropdownList) {
        const wrapper = this._searchWrappers.get(select);
        
        searchInput.style.display = 'block';
        searchInput.value = '';
        searchInput.focus();
        
        // Show search icon
        if (wrapper?.searchIcon) {
            wrapper.searchIcon.style.display = 'block';
        }
        
        this._filterDropdownOptions(select, '', dropdownList);
        dropdownList.style.display = 'block';
        
        // Add active state to select
        select.classList.add('search-active');
    }

    /**
     * Hide search dropdown
     * 
     * @private
     * @param {HTMLInputElement} searchInput - Search input element
     * @param {HTMLElement} dropdownList - Dropdown list element
     * @param {HTMLSelectElement} [select] - Select element (optional)
     */
    _hideSearchDropdown(searchInput, dropdownList, select) {
        searchInput.style.display = 'none';
        dropdownList.style.display = 'none';
        
        // Hide search icon and remove active state
        if (select) {
            const wrapper = this._searchWrappers.get(select);
            if (wrapper?.searchIcon) {
                wrapper.searchIcon.style.display = 'none';
            }
            select.classList.remove('search-active');
        }
    }

    /**
     * Filter dropdown options based on search keyword
     * Supports case-insensitive and diacritics-insensitive search
     * 
     * **Validates: Requirements 7.1, 7.2, 7.3**
     * 
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {string} keyword - Search keyword
     * @param {HTMLElement} dropdownList - Dropdown list element
     */
    _filterDropdownOptions(select, keyword, dropdownList) {
        dropdownList.innerHTML = '';
        
        const normalizedKeyword = this.removeDiacritics(keyword.toLowerCase().trim());
        let hasResults = false;
        let matchCount = 0;

        Array.from(select.options).forEach(option => {
            if (!option.value) return; // Skip placeholder option

            const optionText = option.textContent;
            const normalizedText = this.removeDiacritics(optionText.toLowerCase());

            if (!normalizedKeyword || normalizedText.includes(normalizedKeyword)) {
                hasResults = true;
                matchCount++;
                
                const item = document.createElement('div');
                item.className = 'address-search-item';
                item.style.cssText = `
                    padding: 10px 14px;
                    cursor: pointer;
                    border-bottom: 1px solid #f0f0f0;
                    transition: background-color 0.15s ease;
                    font-size: 14px;
                `;
                
                // Highlight matching text if there's a keyword
                if (normalizedKeyword) {
                    const highlightedText = this._highlightMatch(optionText, keyword);
                    item.innerHTML = highlightedText;
                } else {
                    item.textContent = optionText;
                }
                
                item.dataset.value = option.value;

                item.addEventListener('mouseenter', () => {
                    item.style.backgroundColor = '#f0f7e6';
                    item.style.color = '#81c408';
                });
                item.addEventListener('mouseleave', () => {
                    item.style.backgroundColor = '';
                    item.style.color = '';
                });

                item.addEventListener('click', () => {
                    select.value = option.value;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    
                    const wrapper = this._searchWrappers.get(select);
                    if (wrapper) {
                        this._hideSearchDropdown(wrapper.searchInput, wrapper.dropdownList, select);
                    }
                });

                // Handle keyboard navigation
                item.tabIndex = 0;
                item.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        item.click();
                    } else if (e.key === 'ArrowDown') {
                        e.preventDefault();
                        const next = item.nextElementSibling;
                        if (next && !next.classList.contains('no-results')) {
                            next.focus();
                        }
                    } else if (e.key === 'ArrowUp') {
                        e.preventDefault();
                        const prev = item.previousElementSibling;
                        if (prev && !prev.classList.contains('no-results')) {
                            prev.focus();
                        } else {
                            const wrapper = this._searchWrappers.get(select);
                            if (wrapper) {
                                wrapper.searchInput.focus();
                            }
                        }
                    } else if (e.key === 'Escape') {
                        const wrapper = this._searchWrappers.get(select);
                        if (wrapper) {
                            this._hideSearchDropdown(wrapper.searchInput, wrapper.dropdownList, select);
                            select.focus();
                        }
                    }
                });

                dropdownList.appendChild(item);
            }
        });

        // Show "no results" message (Requirement 7.3)
        if (!hasResults) {
            const noResults = document.createElement('div');
            noResults.className = 'address-search-item no-results';
            noResults.style.cssText = `
                padding: 12px 14px;
                color: #6c757d;
                font-style: italic;
                text-align: center;
                font-size: 14px;
            `;
            noResults.innerHTML = '<i class="fa fa-search me-2"></i>Không tìm thấy kết quả';
            dropdownList.appendChild(noResults);
        } else if (normalizedKeyword) {
            // Show match count when searching
            const countInfo = document.createElement('div');
            countInfo.className = 'address-search-count';
            countInfo.style.cssText = `
                padding: 6px 14px;
                color: #81c408;
                font-size: 12px;
                background: #f8f9fa;
                border-bottom: 1px solid #e9ecef;
            `;
            countInfo.textContent = `Tìm thấy ${matchCount} kết quả`;
            dropdownList.insertBefore(countInfo, dropdownList.firstChild);
        }
    }

    /**
     * Highlight matching text in search results
     * 
     * @private
     * @param {string} text - Original text
     * @param {string} keyword - Search keyword
     * @returns {string} HTML string with highlighted matches
     */
    _highlightMatch(text, keyword) {
        if (!keyword) return text;
        
        const normalizedText = this.removeDiacritics(text.toLowerCase());
        const normalizedKeyword = this.removeDiacritics(keyword.toLowerCase().trim());
        
        const index = normalizedText.indexOf(normalizedKeyword);
        if (index === -1) return text;
        
        // Find the corresponding position in the original text
        const before = text.substring(0, index);
        const match = text.substring(index, index + keyword.length);
        const after = text.substring(index + keyword.length);
        
        return `${before}<strong style="color: #81c408;">${match}</strong>${after}`;
    }

    /**
     * Remove Vietnamese diacritics from string
     * 
     * **Validates: Requirements 7.2**
     * 
     * @param {string} str - Input string
     * @returns {string} String without diacritics
     */
    removeDiacritics(str) {
        if (!str) return '';
        
        const diacriticsMap = {
            'à': 'a', 'á': 'a', 'ả': 'a', 'ã': 'a', 'ạ': 'a',
            'ă': 'a', 'ằ': 'a', 'ắ': 'a', 'ẳ': 'a', 'ẵ': 'a', 'ặ': 'a',
            'â': 'a', 'ầ': 'a', 'ấ': 'a', 'ẩ': 'a', 'ẫ': 'a', 'ậ': 'a',
            'è': 'e', 'é': 'e', 'ẻ': 'e', 'ẽ': 'e', 'ẹ': 'e',
            'ê': 'e', 'ề': 'e', 'ế': 'e', 'ể': 'e', 'ễ': 'e', 'ệ': 'e',
            'ì': 'i', 'í': 'i', 'ỉ': 'i', 'ĩ': 'i', 'ị': 'i',
            'ò': 'o', 'ó': 'o', 'ỏ': 'o', 'õ': 'o', 'ọ': 'o',
            'ô': 'o', 'ồ': 'o', 'ố': 'o', 'ổ': 'o', 'ỗ': 'o', 'ộ': 'o',
            'ơ': 'o', 'ờ': 'o', 'ớ': 'o', 'ở': 'o', 'ỡ': 'o', 'ợ': 'o',
            'ù': 'u', 'ú': 'u', 'ủ': 'u', 'ũ': 'u', 'ụ': 'u',
            'ư': 'u', 'ừ': 'u', 'ứ': 'u', 'ử': 'u', 'ữ': 'u', 'ự': 'u',
            'ỳ': 'y', 'ý': 'y', 'ỷ': 'y', 'ỹ': 'y', 'ỵ': 'y',
            'đ': 'd',
            'À': 'A', 'Á': 'A', 'Ả': 'A', 'Ã': 'A', 'Ạ': 'A',
            'Ă': 'A', 'Ằ': 'A', 'Ắ': 'A', 'Ẳ': 'A', 'Ẵ': 'A', 'Ặ': 'A',
            'Â': 'A', 'Ầ': 'A', 'Ấ': 'A', 'Ẩ': 'A', 'Ẫ': 'A', 'Ậ': 'A',
            'È': 'E', 'É': 'E', 'Ẻ': 'E', 'Ẽ': 'E', 'Ẹ': 'E',
            'Ê': 'E', 'Ề': 'E', 'Ế': 'E', 'Ể': 'E', 'Ễ': 'E', 'Ệ': 'E',
            'Ì': 'I', 'Í': 'I', 'Ỉ': 'I', 'Ĩ': 'I', 'Ị': 'I',
            'Ò': 'O', 'Ó': 'O', 'Ỏ': 'O', 'Õ': 'O', 'Ọ': 'O',
            'Ô': 'O', 'Ồ': 'O', 'Ố': 'O', 'Ổ': 'O', 'Ỗ': 'O', 'Ộ': 'O',
            'Ơ': 'O', 'Ờ': 'O', 'Ớ': 'O', 'Ở': 'O', 'Ỡ': 'O', 'Ợ': 'O',
            'Ù': 'U', 'Ú': 'U', 'Ủ': 'U', 'Ũ': 'U', 'Ụ': 'U',
            'Ư': 'U', 'Ừ': 'U', 'Ứ': 'U', 'Ử': 'U', 'Ữ': 'U', 'Ự': 'U',
            'Ỳ': 'Y', 'Ý': 'Y', 'Ỷ': 'Y', 'Ỹ': 'Y', 'Ỵ': 'Y',
            'Đ': 'D'
        };

        return str.split('').map(char => diacriticsMap[char] || char).join('');
    }

    /**
     * Filter list by keyword (case-insensitive, diacritics-insensitive)
     * 
     * @param {Array} list - List of items with 'name' property
     * @param {string} keyword - Search keyword
     * @returns {Array} Filtered list
     */
    filterByKeyword(list, keyword) {
        if (!keyword || !keyword.trim()) {
            return list;
        }

        const normalizedKeyword = this.removeDiacritics(keyword.toLowerCase().trim());
        
        return list.filter(item => {
            const normalizedName = this.removeDiacritics(item.name.toLowerCase());
            return normalizedName.includes(normalizedKeyword);
        });
    }

    // ==================== ADDRESS COMPOSITION ====================

    /**
     * Compose full address from components
     * Format: "{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}"
     * 
     * @returns {string} Full composed address
     */
    composeFullAddress() {
        const streetAddress = this.streetAddressInput?.value?.trim() || '';
        const wardName = this.wardSelect?.selectedOptions[0]?.text || '';
        const districtName = this.districtSelect?.selectedOptions[0]?.text || '';
        const provinceName = this.provinceSelect?.selectedOptions[0]?.text || '';

        const parts = [streetAddress, wardName, districtName, provinceName]
            .filter(part => part && part !== '-- Chọn --');

        return parts.join(', ');
    }

    /**
     * Restore saved address to dropdowns
     * Loads cascading dropdowns in order: Province → District → Ward
     * 
     * **Validates: Requirements 8.1, 8.2, 8.3**
     * 
     * @param {Object} savedAddress - Saved address object
     * @param {number} savedAddress.provinceCode - Province code
     * @param {string} [savedAddress.provinceName] - Province name
     * @param {number} savedAddress.districtCode - District code
     * @param {string} [savedAddress.districtName] - District name
     * @param {number} savedAddress.wardCode - Ward code
     * @param {string} [savedAddress.wardName] - Ward name
     * @param {string} savedAddress.streetAddress - Street address
     * @returns {Promise<boolean>} True if restore was successful
     */
    async restoreSavedAddress(savedAddress) {
        if (!savedAddress) return false;

        let restoreSuccess = true;
        let warnings = [];

        try {
            // Load and select province (Requirement 8.2 - load in order)
            if (savedAddress.provinceCode) {
                await this.loadProvinces();
                
                // Check if province exists in current data (Requirement 8.3)
                const provinceExists = this._selectOption(this.provinceSelect, savedAddress.provinceCode);
                if (!provinceExists && savedAddress.provinceName) {
                    warnings.push(`Tỉnh/Thành phố "${savedAddress.provinceName}" không còn trong danh sách`);
                    restoreSuccess = false;
                }

                // Load and select district
                if (provinceExists && savedAddress.districtCode) {
                    await this.loadDistricts(savedAddress.provinceCode);
                    
                    // Check if district exists (Requirement 8.3)
                    const districtExists = this._selectOption(this.districtSelect, savedAddress.districtCode);
                    if (!districtExists && savedAddress.districtName) {
                        warnings.push(`Quận/Huyện "${savedAddress.districtName}" không còn trong danh sách`);
                        restoreSuccess = false;
                    }

                    // Load and select ward
                    if (districtExists && savedAddress.wardCode) {
                        await this.loadWards(savedAddress.districtCode);
                        
                        // Check if ward exists (Requirement 8.3)
                        const wardExists = this._selectOption(this.wardSelect, savedAddress.wardCode);
                        if (!wardExists && savedAddress.wardName) {
                            warnings.push(`Phường/Xã "${savedAddress.wardName}" không còn trong danh sách`);
                            restoreSuccess = false;
                        }
                    }
                }
            }

            // Set street address
            if (savedAddress.streetAddress && this.streetAddressInput) {
                this.streetAddressInput.value = savedAddress.streetAddress;
            }

            // Show warning if address components are missing (Requirement 8.3)
            if (warnings.length > 0) {
                this._showAddressWarning(warnings);
            }

            return restoreSuccess;
        } catch (error) {
            this._handleError('Không thể khôi phục địa chỉ đã lưu', error);
            return false;
        }
    }

    /**
     * Show warning when saved address is no longer valid
     * 
     * @private
     * @param {string[]} warnings - List of warning messages
     */
    _showAddressWarning(warnings) {
        const message = 'Một số thông tin địa chỉ đã thay đổi:\n' + warnings.join('\n') + '\n\nVui lòng chọn lại.';
        
        if (this.options.onError) {
            this.options.onError(message, new Error('Address components changed'));
        } else {
            console.warn(message);
            // Create a visual warning if no error handler
            const warningDiv = document.createElement('div');
            warningDiv.className = 'alert alert-warning address-restore-warning';
            warningDiv.innerHTML = `<i class="fa fa-exclamation-triangle me-2"></i>${warnings.join('<br>')}`;
            warningDiv.style.cssText = 'margin-top: 10px; font-size: 0.875rem;';
            
            // Insert after the ward select wrapper
            const wardWrapper = this._searchWrappers.get(this.wardSelect)?.wrapper || this.wardSelect?.parentNode;
            if (wardWrapper) {
                const existingWarning = wardWrapper.parentNode.querySelector('.address-restore-warning');
                if (existingWarning) {
                    existingWarning.remove();
                }
                wardWrapper.parentNode.insertBefore(warningDiv, wardWrapper.nextSibling);
            }
        }
    }


    // ==================== PRIVATE HELPER METHODS ====================

    /**
     * Bind event handlers to dropdowns
     * Implements cascading reset logic
     * 
     * **Validates: Requirements 2.3, 3.3**
     * 
     * @private
     */
    _bindEvents() {
        if (this.provinceSelect) {
            this.provinceSelect.addEventListener('change', async (e) => {
                const provinceCode = parseInt(e.target.value);
                const selectedOption = e.target.selectedOptions[0];
                
                // Update hidden name input
                if (this.provinceNameInput) {
                    this.provinceNameInput.value = selectedOption?.text || '';
                }
                
                // Reset child dropdowns (cascading reset - Requirement 2.3)
                this._resetDropdown(this.districtSelect, '-- Chọn Quận/Huyện --');
                this._resetDropdown(this.wardSelect, '-- Chọn Phường/Xã --');
                this._disableDropdown(this.districtSelect);
                this._disableDropdown(this.wardSelect);
                
                // Clear hidden name inputs for child dropdowns
                if (this.districtNameInput) this.districtNameInput.value = '';
                if (this.wardNameInput) this.wardNameInput.value = '';

                if (provinceCode) {
                    await this.loadDistricts(provinceCode);
                }

                if (this.options.onProvinceChange) {
                    this.options.onProvinceChange(provinceCode, selectedOption?.text);
                }
            });
        }

        if (this.districtSelect) {
            this.districtSelect.addEventListener('change', async (e) => {
                const districtCode = parseInt(e.target.value);
                const selectedOption = e.target.selectedOptions[0];
                
                // Update hidden name input
                if (this.districtNameInput) {
                    this.districtNameInput.value = selectedOption?.text || '';
                }
                
                // Reset ward dropdown (cascading reset - Requirement 3.3)
                this._resetDropdown(this.wardSelect, '-- Chọn Phường/Xã --');
                this._disableDropdown(this.wardSelect);
                
                // Clear hidden name input for ward
                if (this.wardNameInput) this.wardNameInput.value = '';

                if (districtCode) {
                    await this.loadWards(districtCode);
                }

                if (this.options.onDistrictChange) {
                    this.options.onDistrictChange(districtCode, selectedOption?.text);
                }
            });
        }

        if (this.wardSelect) {
            this.wardSelect.addEventListener('change', (e) => {
                const wardCode = parseInt(e.target.value);
                const selectedOption = e.target.selectedOptions[0];
                
                // Update hidden name input
                if (this.wardNameInput) {
                    this.wardNameInput.value = selectedOption?.text || '';
                }
                
                if (this.options.onWardChange) {
                    this.options.onWardChange(wardCode, selectedOption?.text);
                }
            });
        }
    }

    /**
     * Populate province dropdown with data
     * @private
     * @param {Array} provinces - List of provinces
     */
    _populateProvinceDropdown(provinces) {
        if (!this.provinceSelect) return;

        const currentValue = this.provinceSelect.value;
        this.provinceSelect.innerHTML = '<option value="">-- Chọn Tỉnh/Thành phố --</option>';
        
        provinces.forEach(province => {
            const option = document.createElement('option');
            option.value = province.code;
            option.textContent = province.name;
            this.provinceSelect.appendChild(option);
        });

        // Restore previous selection if exists
        if (currentValue) {
            this.provinceSelect.value = currentValue;
        }
    }

    /**
     * Populate district dropdown with data
     * @private
     * @param {Array} districts - List of districts
     */
    _populateDistrictDropdown(districts) {
        if (!this.districtSelect) return;

        const currentValue = this.districtSelect.value;
        this.districtSelect.innerHTML = '<option value="">-- Chọn Quận/Huyện --</option>';
        
        districts.forEach(district => {
            const option = document.createElement('option');
            option.value = district.code;
            option.textContent = district.name;
            this.districtSelect.appendChild(option);
        });

        // Restore previous selection if exists
        if (currentValue) {
            this.districtSelect.value = currentValue;
        }
    }

    /**
     * Populate ward dropdown with data
     * @private
     * @param {Array} wards - List of wards
     */
    _populateWardDropdown(wards) {
        if (!this.wardSelect) return;

        const currentValue = this.wardSelect.value;
        this.wardSelect.innerHTML = '<option value="">-- Chọn Phường/Xã --</option>';
        
        wards.forEach(ward => {
            const option = document.createElement('option');
            option.value = ward.code;
            option.textContent = ward.name;
            this.wardSelect.appendChild(option);
        });

        // Restore previous selection if exists
        if (currentValue) {
            this.wardSelect.value = currentValue;
        }
    }

    /**
     * Reset dropdown to default state
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {string} [placeholder='-- Chọn --'] - Placeholder text
     */
    _resetDropdown(select, placeholder = '-- Chọn --') {
        if (!select) return;
        select.innerHTML = `<option value="">${placeholder}</option>`;
        select.value = '';
    }

    /**
     * Show "no data" message in dropdown
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {string} message - Message to display
     */
    _showNoData(select, message) {
        if (!select) return;
        select.innerHTML = `<option value="">${message}</option>`;
        select.disabled = true;
    }

    /**
     * Enable manual input fallback when API fails
     * @private
     */
    _enableManualInput() {
        // This could be extended to show text inputs instead of dropdowns
        // For now, just enable the dropdowns with a message
        if (this.provinceSelect) {
            this.provinceSelect.innerHTML = '<option value="">-- Không thể tải dữ liệu --</option>';
            this.provinceSelect.disabled = false;
        }
    }

    /**
     * Enable dropdown
     * @private
     * @param {HTMLSelectElement} select - Select element
     */
    _enableDropdown(select) {
        if (!select) return;
        select.disabled = false;
        select.classList.remove('disabled');
    }

    /**
     * Disable dropdown
     * @private
     * @param {HTMLSelectElement} select - Select element
     */
    _disableDropdown(select) {
        if (!select) return;
        select.disabled = true;
        select.classList.add('disabled');
    }

    /**
     * Show loading indicator on dropdown
     * @private
     * @param {HTMLSelectElement} select - Select element
     */
    _showLoading(select) {
        if (!select) return;
        select.classList.add('loading');
        select.disabled = true;
    }

    /**
     * Hide loading indicator on dropdown
     * @private
     * @param {HTMLSelectElement} select - Select element
     */
    _hideLoading(select) {
        if (!select) return;
        select.classList.remove('loading');
    }

    /**
     * Select option by value
     * @private
     * @param {HTMLSelectElement} select - Select element
     * @param {*} value - Value to select
     * @returns {boolean} True if option was found and selected
     */
    _selectOption(select, value) {
        if (!select || !value) return false;
        
        const valueStr = value.toString();
        const option = Array.from(select.options).find(opt => opt.value === valueStr);
        
        if (option) {
            select.value = valueStr;
            // Trigger change event to update hidden name inputs
            select.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
        }
        
        return false;
    }

    /**
     * Handle error
     * @private
     * @param {string} message - Error message
     * @param {Error} error - Error object
     */
    _handleError(message, error) {
        console.error(message, error);
        
        if (this.options.onError) {
            this.options.onError(message, error);
        }
    }

    /**
     * Initialize the handler and load provinces
     * Call this after creating the instance to start loading data
     * 
     * @returns {Promise<void>}
     */
    async init() {
        await this.loadProvinces();
        
        if (this.options.onLoad) {
            this.options.onLoad();
        }
    }

    /**
     * Get current address values from the form
     * 
     * @returns {Object} Current address values
     */
    getAddressValues() {
        return {
            provinceCode: parseInt(this.provinceSelect?.value) || 0,
            provinceName: this.provinceNameInput?.value || this.provinceSelect?.selectedOptions[0]?.text || '',
            districtCode: parseInt(this.districtSelect?.value) || 0,
            districtName: this.districtNameInput?.value || this.districtSelect?.selectedOptions[0]?.text || '',
            wardCode: parseInt(this.wardSelect?.value) || 0,
            wardName: this.wardNameInput?.value || this.wardSelect?.selectedOptions[0]?.text || '',
            streetAddress: this.streetAddressInput?.value?.trim() || '',
            fullAddress: this.composeFullAddress()
        };
    }

    /**
     * Validate that all required address fields are filled
     * 
     * @returns {Object} Validation result with isValid and errors
     */
    validate() {
        const errors = [];
        const values = this.getAddressValues();

        if (!values.provinceCode) {
            errors.push('Vui lòng chọn Tỉnh/Thành phố');
        }
        if (!values.districtCode) {
            errors.push('Vui lòng chọn Quận/Huyện');
        }
        if (!values.wardCode) {
            errors.push('Vui lòng chọn Phường/Xã');
        }
        if (!values.streetAddress) {
            errors.push('Vui lòng nhập số nhà, tên đường');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AddressHandler;
}
