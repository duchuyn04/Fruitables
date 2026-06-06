class AddressHandler {
    static DEFAULT_TTL_HOURS = 24;
    static CACHE_PREFIX = 'address_v2_cache_';

    constructor(options) {
        this.options = {
            apiBaseUrl: '/api/address',
            enableSearch: true,
            ...options
        };

        this.provinceSelect = document.querySelector(this.options.provinceSelector);
        this.communeSelect = document.querySelector(this.options.communeSelector);
        this.streetAddressInput = document.querySelector(this.options.streetAddressSelector);

        this.provinceNameInput = this.options.provinceNameSelector ?
            document.querySelector(this.options.provinceNameSelector) : null;
        this.communeNameInput = this.options.communeNameSelector ?
            document.querySelector(this.options.communeNameSelector) : null;

        this._provincesData = [];
        this._communesData = [];

        this._searchWrappers = new Map();

        this._bindEvents();

        if (this.options.enableSearch) {
            this._initializeSearch();
        }
    }

    // ==================== CACHING METHODS ====================

    getFromCache(key) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;

        try {
            const cached = localStorage.getItem(cacheKey);
            if (!cached) {
                return null;
            }

            const entry = JSON.parse(cached);
            const now = Date.now();

            if (now > entry.expiresAt) {
                localStorage.removeItem(cacheKey);
                return null;
            }

            return entry.data;
        } catch (error) {
            console.warn('Error reading from cache:', error);
            return null;
        }
    }

    setToCache(key, data, ttlHours = AddressHandler.DEFAULT_TTL_HOURS) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;
        const now = Date.now();
        const ttlMs = ttlHours * 60 * 60 * 1000;

        const entry = {
            data: data,
            storedAt: now,
            expiresAt: now + ttlMs
        };

        try {
            localStorage.setItem(cacheKey, JSON.stringify(entry));
        } catch (error) {
            console.warn('Error writing to cache:', error);
            this._clearExpiredCache();
            try {
                localStorage.setItem(cacheKey, JSON.stringify(entry));
            } catch (retryError) {
                console.error('Failed to write to cache after cleanup:', retryError);
            }
        }
    }

    hasValidCache(key) {
        return this.getFromCache(key) !== null;
    }

    invalidateCache(key) {
        const cacheKey = AddressHandler.CACHE_PREFIX + key;
        try {
            localStorage.removeItem(cacheKey);
        } catch (error) {
            console.warn('Error invalidating cache:', error);
        }
    }

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

    async loadProvinces() {
        const cacheKey = 'provinces';

        const cached = this.getFromCache(cacheKey);
        if (cached) {
            this._provincesData = cached;
            this._populateProvinceDropdown(cached);
            this._enableDropdown(this.provinceSelect);
            return cached;
        }

        try {
            this._showLoading(this.provinceSelect);

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

    async loadCommunes(provinceId) {
        if (!provinceId) {
            this._resetDropdown(this.communeSelect);
            this._disableDropdown(this.communeSelect);
            return [];
        }

        const cacheKey = `communes_${provinceId}`;

        const cached = this.getFromCache(cacheKey);
        if (cached) {
            this._communesData = cached;
            this._populateCommuneDropdown(cached);
            this._enableDropdown(this.communeSelect);
            return cached;
        }

        try {
            this._showLoading(this.communeSelect);

            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 10000);

            const response = await fetch(`${this.options.apiBaseUrl}/communes/${provinceId}`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const communes = await response.json();

            if (!communes || communes.length === 0) {
                this._showNoData(this.communeSelect, 'Không có dữ liệu');
                return [];
            }

            this.setToCache(cacheKey, communes);

            this._communesData = communes;
            this._populateCommuneDropdown(communes);
            this._enableDropdown(this.communeSelect);
            return communes;
        } catch (error) {
            this._handleError('Không thể tải danh sách phường/xã', error);
            return [];
        } finally {
            this._hideLoading(this.communeSelect);
        }
    }

    // ==================== SEARCH/FILTER METHODS ====================

    _initializeSearch() {
        [this.provinceSelect, this.communeSelect].forEach(select => {
            if (select) {
                this._createSearchWrapper(select);
            }
        });
    }

    _createSearchWrapper(select) {
        const wrapper = document.createElement('div');
        wrapper.className = 'address-search-wrapper';
        wrapper.style.cssText = 'position: relative;';

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

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        wrapper.appendChild(searchIcon);
        wrapper.appendChild(searchInput);
        wrapper.appendChild(dropdownList);

        this._searchWrappers.set(select, { wrapper, searchInput, dropdownList, searchIcon });

        this._bindSearchEvents(select, searchInput, dropdownList);

        this._addSearchIndicator(select);
    }

    _addSearchIndicator(select) {
        select.title = 'Nhấp để tìm kiếm';
        select.style.cursor = 'pointer';
    }

    _bindSearchEvents(select, searchInput, dropdownList) {
        let isOpen = false;

        select.addEventListener('mousedown', (e) => {
            if (!select.disabled && select.options.length > 1) {
                e.preventDefault();
                e.stopPropagation();
            }
        });

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

        let searchTimeout;
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                this._filterDropdownOptions(select, searchInput.value, dropdownList);
            }, 100);
        });

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

        document.addEventListener('click', (e) => {
            const wrapper = this._searchWrappers.get(select)?.wrapper;
            if (wrapper && !wrapper.contains(e.target)) {
                this._hideSearchDropdown(searchInput, dropdownList, select);
                isOpen = false;
            }
        });
    }

    _showSearchDropdown(select, searchInput, dropdownList) {
        const wrapper = this._searchWrappers.get(select);

        searchInput.style.display = 'block';
        searchInput.value = '';
        searchInput.focus();

        if (wrapper?.searchIcon) {
            wrapper.searchIcon.style.display = 'block';
        }

        this._filterDropdownOptions(select, '', dropdownList);
        dropdownList.style.display = 'block';

        select.classList.add('search-active');
    }

    _hideSearchDropdown(searchInput, dropdownList, select) {
        searchInput.style.display = 'none';
        dropdownList.style.display = 'none';

        if (select) {
            const wrapper = this._searchWrappers.get(select);
            if (wrapper?.searchIcon) {
                wrapper.searchIcon.style.display = 'none';
            }
            select.classList.remove('search-active');
        }
    }

    _filterDropdownOptions(select, keyword, dropdownList) {
        dropdownList.innerHTML = '';

        const normalizedKeyword = this.removeDiacritics(keyword.toLowerCase().trim());
        let hasResults = false;
        let matchCount = 0;

        Array.from(select.options).forEach(option => {
            if (!option.value) return;

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

    _highlightMatch(text, keyword) {
        if (!keyword) return text;

        const normalizedText = this.removeDiacritics(text.toLowerCase());
        const normalizedKeyword = this.removeDiacritics(keyword.toLowerCase().trim());

        const index = normalizedText.indexOf(normalizedKeyword);
        if (index === -1) return text;

        const before = text.substring(0, index);
        const match = text.substring(index, index + keyword.length);
        const after = text.substring(index + keyword.length);

        return `${before}<strong style="color: #81c408;">${match}</strong>${after}`;
    }

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

    composeFullAddress() {
        const streetAddress = this.streetAddressInput?.value?.trim() || '';
        const communeName = this.communeSelect?.selectedOptions[0]?.text || '';
        const provinceName = this.provinceSelect?.selectedOptions[0]?.text || '';

        const parts = [streetAddress, communeName, provinceName]
            .filter(part => part && part !== '-- Chọn --');

        return parts.join(', ');
    }

    async restoreSavedAddress(savedAddress) {
        if (!savedAddress) return false;

        let restoreSuccess = true;
        let warnings = [];

        try {
            if (savedAddress.provinceCode) {
                await this.loadProvinces();

                const provinceExists = this._selectOption(this.provinceSelect, savedAddress.provinceCode);
                if (!provinceExists && savedAddress.provinceName) {
                    warnings.push(`Tỉnh/Thành phố "${savedAddress.provinceName}" không còn trong danh sách`);
                    restoreSuccess = false;
                }

                if (provinceExists && savedAddress.communeCode) {
                    await this.loadCommunes(savedAddress.provinceCode);

                    const communeExists = this._selectOption(this.communeSelect, savedAddress.communeCode);
                    if (!communeExists && savedAddress.communeName) {
                        warnings.push(`Phường/Xã "${savedAddress.communeName}" không còn trong danh sách`);
                        restoreSuccess = false;
                    }
                }
            }

            if (savedAddress.streetAddress && this.streetAddressInput) {
                this.streetAddressInput.value = savedAddress.streetAddress;
            }

            if (warnings.length > 0) {
                this._showAddressWarning(warnings);
            }

            return restoreSuccess;
        } catch (error) {
            this._handleError('Không thể khôi phục địa chỉ đã lưu', error);
            return false;
        }
    }

    _showAddressWarning(warnings) {
        const message = 'Một số thông tin địa chỉ đã thay đổi:\n' + warnings.join('\n') + '\n\nVui lòng chọn lại.';

        if (this.options.onError) {
            this.options.onError(message, new Error('Address components changed'));
        } else {
            console.warn(message);
            const warningDiv = document.createElement('div');
            warningDiv.className = 'alert alert-warning address-restore-warning';
            warningDiv.innerHTML = `<i class="fa fa-exclamation-triangle me-2"></i>${warnings.join('<br>')}`;
            warningDiv.style.cssText = 'margin-top: 10px; font-size: 0.875rem;';

            const communeWrapper = this._searchWrappers.get(this.communeSelect)?.wrapper || this.communeSelect?.parentNode;
            if (communeWrapper) {
                const existingWarning = communeWrapper.parentNode.querySelector('.address-restore-warning');
                if (existingWarning) {
                    existingWarning.remove();
                }
                communeWrapper.parentNode.insertBefore(warningDiv, communeWrapper.nextSibling);
            }
        }
    }

    // ==================== PRIVATE HELPER METHODS ====================

    _bindEvents() {
        if (this.provinceSelect) {
            this.provinceSelect.addEventListener('change', async (e) => {
                const provinceCode = e.target.value;
                const selectedOption = e.target.selectedOptions[0];

                if (this.provinceNameInput) {
                    this.provinceNameInput.value = selectedOption?.text || '';
                }

                this._resetDropdown(this.communeSelect, '-- Chọn Phường/Xã --');
                this._disableDropdown(this.communeSelect);

                if (this.communeNameInput) this.communeNameInput.value = '';

                if (provinceCode) {
                    await this.loadCommunes(provinceCode);
                }

                if (this.options.onProvinceChange) {
                    this.options.onProvinceChange(provinceCode, selectedOption?.text);
                }
            });
        }

        if (this.communeSelect) {
            this.communeSelect.addEventListener('change', (e) => {
                const communeCode = e.target.value;
                const selectedOption = e.target.selectedOptions[0];

                if (this.communeNameInput) {
                    this.communeNameInput.value = selectedOption?.text || '';
                }

                if (this.options.onCommuneChange) {
                    this.options.onCommuneChange(communeCode, selectedOption?.text);
                }
            });
        }
    }

    _populateProvinceDropdown(provinces) {
        if (!this.provinceSelect) return;

        const currentValue = this.provinceSelect.value;
        this.provinceSelect.innerHTML = '<option value="">-- Chọn Tỉnh/Thành phố --</option>';

        provinces.forEach(province => {
            const option = document.createElement('option');
            option.value = province.id || province.code;
            option.textContent = province.name;
            this.provinceSelect.appendChild(option);
        });

        if (currentValue) {
            this.provinceSelect.value = currentValue;
        }
    }

    _populateCommuneDropdown(communes) {
        if (!this.communeSelect) return;

        const currentValue = this.communeSelect.value;
        this.communeSelect.innerHTML = '<option value="">-- Chọn Phường/Xã --</option>';

        communes.forEach(commune => {
            const option = document.createElement('option');
            option.value = commune.id || commune.code;
            option.textContent = commune.name;
            this.communeSelect.appendChild(option);
        });

        if (currentValue) {
            this.communeSelect.value = currentValue;
        }
    }

    _resetDropdown(select, placeholder = '-- Chọn --') {
        if (!select) return;
        select.innerHTML = `<option value="">${placeholder}</option>`;
        select.value = '';
    }

    _showNoData(select, message) {
        if (!select) return;
        select.innerHTML = `<option value="">${message}</option>`;
        select.disabled = true;
    }

    _enableManualInput() {
        if (this.provinceSelect) {
            this.provinceSelect.innerHTML = '<option value="">-- Không thể tải dữ liệu --</option>';
            this.provinceSelect.disabled = false;
        }
    }

    _enableDropdown(select) {
        if (!select) return;
        select.disabled = false;
        select.classList.remove('disabled');
    }

    _disableDropdown(select) {
        if (!select) return;
        select.disabled = true;
        select.classList.add('disabled');
    }

    _showLoading(select) {
        if (!select) return;
        select.classList.add('loading');
        select.disabled = true;
    }

    _hideLoading(select) {
        if (!select) return;
        select.classList.remove('loading');
    }

    _selectOption(select, value) {
        if (!select || !value) return false;

        const valueStr = value.toString();
        const option = Array.from(select.options).find(opt => opt.value === valueStr);

        if (option) {
            select.value = valueStr;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
        }

        return false;
    }

    _handleError(message, error) {
        console.error(message, error);

        if (this.options.onError) {
            this.options.onError(message, error);
        }
    }

    async init() {
        await this.loadProvinces();

        if (this.options.onLoad) {
            this.options.onLoad();
        }
    }

    getAddressValues() {
        return {
            provinceCode: this.provinceSelect?.value || '',
            provinceName: this.provinceNameInput?.value || this.provinceSelect?.selectedOptions[0]?.text || '',
            communeCode: this.communeSelect?.value || '',
            communeName: this.communeNameInput?.value || this.communeSelect?.selectedOptions[0]?.text || '',
            streetAddress: this.streetAddressInput?.value?.trim() || '',
            fullAddress: this.composeFullAddress()
        };
    }

    validate() {
        const errors = [];
        const values = this.getAddressValues();

        if (!values.provinceCode) {
            errors.push('Vui lòng chọn Tỉnh/Thành phố');
        }
        if (!values.communeCode) {
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

if (typeof module !== 'undefined' && module.exports) {
    module.exports = AddressHandler;
}
