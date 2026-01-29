/**
 * Cart Storage - Lưu giỏ hàng vào localStorage
 * Giỏ hàng được lưu theo user (nếu đăng nhập) hoặc guest
 */
const CartStorage = {
    STORAGE_KEY: 'fruitables_cart',
    
    /**
     * Lấy key lưu trữ dựa trên userId
     */
    getStorageKey: function(userId) {
        return userId ? `${this.STORAGE_KEY}_user_${userId}` : `${this.STORAGE_KEY}_guest`;
    },
    
    /**
     * Lấy giỏ hàng từ localStorage
     */
    getCart: function(userId) {
        const key = this.getStorageKey(userId);
        const data = localStorage.getItem(key);
        if (data) {
            try {
                return JSON.parse(data);
            } catch (e) {
                console.error('Error parsing cart data:', e);
                return { items: [] };
            }
        }
        return { items: [] };
    },
    
    /**
     * Lưu giỏ hàng vào localStorage
     */
    saveCart: function(userId, cart) {
        const key = this.getStorageKey(userId);
        try {
            localStorage.setItem(key, JSON.stringify(cart));
        } catch (e) {
            console.error('Error saving cart:', e);
        }
    },
    
    /**
     * Thêm sản phẩm vào giỏ hàng
     */
    addItem: function(userId, productId, productName, price, quantity, image) {
        const cart = this.getCart(userId);
        const existingItem = cart.items.find(item => item.productId === productId);
        
        if (existingItem) {
            existingItem.quantity += quantity;
        } else {
            cart.items.push({
                productId: productId,
                productName: productName,
                price: price,
                quantity: quantity,
                image: image
            });
        }
        
        this.saveCart(userId, cart);
        this.updateCartCount(userId);
        return cart;
    },
    
    /**
     * Cập nhật số lượng sản phẩm
     */
    updateQuantity: function(userId, productId, quantity) {
        const cart = this.getCart(userId);
        const item = cart.items.find(item => item.productId === productId);
        
        if (item) {
            if (quantity <= 0) {
                cart.items = cart.items.filter(i => i.productId !== productId);
            } else {
                item.quantity = quantity;
            }
        }
        
        this.saveCart(userId, cart);
        this.updateCartCount(userId);
        return cart;
    },
    
    /**
     * Xóa sản phẩm khỏi giỏ hàng
     */
    removeItem: function(userId, productId) {
        const cart = this.getCart(userId);
        cart.items = cart.items.filter(item => item.productId !== productId);
        this.saveCart(userId, cart);
        this.updateCartCount(userId);
        return cart;
    },
    
    /**
     * Xóa toàn bộ giỏ hàng
     */
    clearCart: function(userId) {
        const key = this.getStorageKey(userId);
        localStorage.removeItem(key);
        this.updateCartCount(userId);
    },
    
    /**
     * Xóa giỏ hàng guest (khi đăng xuất)
     */
    clearGuestCart: function() {
        localStorage.removeItem(this.getStorageKey(null));
    },
    
    /**
     * Lấy tổng số lượng sản phẩm trong giỏ
     */
    getCartCount: function(userId) {
        const cart = this.getCart(userId);
        return cart.items.reduce((total, item) => total + item.quantity, 0);
    },
    
    /**
     * Cập nhật hiển thị số lượng giỏ hàng trên UI
     */
    updateCartCount: function(userId) {
        const count = this.getCartCount(userId);
        const cartCountElements = document.querySelectorAll('.cart-count, #cart-count');
        cartCountElements.forEach(el => {
            el.textContent = count;
            el.style.display = count > 0 ? 'inline-block' : 'none';
        });
    },
    
    /**
     * Merge giỏ hàng guest vào giỏ hàng user khi đăng nhập
     */
    mergeGuestCartToUser: function(userId) {
        const guestCart = this.getCart(null);
        if (guestCart.items.length === 0) return;
        
        const userCart = this.getCart(userId);
        
        guestCart.items.forEach(guestItem => {
            const existingItem = userCart.items.find(item => item.productId === guestItem.productId);
            if (existingItem) {
                existingItem.quantity += guestItem.quantity;
            } else {
                userCart.items.push(guestItem);
            }
        });
        
        this.saveCart(userId, userCart);
        this.clearGuestCart();
        this.updateCartCount(userId);
    },
    
    /**
     * Lấy tổng tiền giỏ hàng
     */
    getSubtotal: function(userId) {
        const cart = this.getCart(userId);
        return cart.items.reduce((total, item) => total + (item.price * item.quantity), 0);
    },
    
    /**
     * Khởi tạo - cập nhật cart count khi trang load
     */
    init: function(userId) {
        document.addEventListener('DOMContentLoaded', () => {
            this.updateCartCount(userId);
        });
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CartStorage;
}
