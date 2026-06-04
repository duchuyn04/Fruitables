# SignalR Manual Testing Scenarios

Dưới đây là một số kịch bản test thủ công (Manual Testing Scenarios) bao quát toàn bộ tính năng SignalR mà chúng ta vừa tích hợp. Bạn có thể sử dụng 2 trình duyệt khác nhau (hoặc 1 trình duyệt thường và 1 tab ẩn danh) để giả lập tương tác giữa các user/admin.

### Kịch bản 1: Cập nhật Tồn kho Realtime (Stock Updates)
**Mục tiêu:** Đảm bảo khi một user đặt hàng (hoặc admin sửa kho), số lượng tồn kho hiển thị tự động cập nhật ở các client đang mở trang sản phẩm mà không cần F5.
1. **Trình duyệt A (Guest/Anonymous):** Mở trang chi tiết của sản phẩm X (VD: Táo, hiện đang có tồn kho = 10). Không đăng nhập.
2. **Trình duyệt B (Customer đang đăng nhập):** 
   - Thêm sản phẩm X vào giỏ hàng (VD: Số lượng 2).
   - Tiến hành Checkout và đặt hàng thành công.
3. **Kiểm tra ở Trình duyệt A:** Ngay khi Trình duyệt B báo đặt hàng thành công, hãy nhìn vào số lượng tồn kho hiển thị ở Trình duyệt A. Nó phải tự động tụt xuống 8. Đồng thời, một Toast (thông báo nhỏ) sẽ bật lên báo "Số lượng sản phẩm trong kho vừa được cập nhật".

### Kịch bản 2: Cập nhật Trạng thái Đơn hàng và Thanh toán Realtime
**Mục tiêu:** Đảm bảo khi Admin thay đổi trạng thái đơn hàng thì Customer đang xem chi tiết đơn đó sẽ tự động được cập nhật.
1. **Trình duyệt A (Customer):** Đăng nhập và đi tới Lịch sử mua hàng (Order History). Nhấp vào xem chi tiết một đơn hàng đang ở trạng thái `Pending`.
2. **Trình duyệt B (Admin):** Đăng nhập quyền Admin. Vào Admin Panel > Quản lý Order. Mở chi tiết cùng đơn hàng đó và tiến hành:
   - Đổi trạng thái từ `Pending` sang `Processing`.
   - Nhấn Save.
3. **Kiểm tra ở Trình duyệt A:** Trang chi tiết đơn hàng của Customer sẽ tự động reload/cập nhật lại và hiển thị trạng thái mới là `Processing` ngay lập tức.
*(Tương tự, Admin có thể đổi trạng thái thanh toán từ `Pending` sang `Paid` để xem trang Customer có tự động nhận event không).*

### Kịch bản 3: Thêm Ghi chú Đơn hàng (Order Notes) Realtime
**Mục tiêu:** Cả Admin và Customer đều có thể trao đổi ghi chú đơn hàng trong thời gian thực.
1. **Trình duyệt A (Customer):** Đang mở trang chi tiết đơn hàng của mình.
2. **Trình duyệt B (Admin):** Đang mở trang chi tiết của đơn hàng đó trong Admin Panel.
3. **Thao tác:** Ở trình duyệt B (Admin), gõ một Note mới và bấm Add Note.
4. **Kiểm tra ở Trình duyệt A:** Trang của Customer sẽ tự nhận sự kiện `OrderNoteAdded` và tự reload để hiển thị ghi chú mới từ Admin.

### Kịch bản 4: Notification Đơn hàng mới cho Admin
**Mục tiêu:** Đảm bảo toàn bộ Admin online đều nhận được thông báo khi có đơn hàng mới phát sinh.
1. **Trình duyệt A (Admin):** Đang đăng nhập và mở Dashboard của Admin (hoặc mở bất kỳ tab nào trong Admin Panel).
2. **Trình duyệt B (Customer):** Tiến hành Checkout một giỏ hàng mới.
3. **Kiểm tra ở Trình duyệt A:** Sau khi Customer đặt hàng thành công, ở góc màn hình của Admin sẽ ngay lập tức hiện một Toast notification báo "Đơn hàng #ORD-XXX vừa được tạo mới!". Admin có thể click vào để xem.

### Kịch bản 5: Bảo mật và Phân quyền (Security)
**Mục tiêu:** Đảm bảo không user nào có thể xem lén trạng thái hoặc can thiệp nhóm SignalR đơn hàng của người khác.
1. Mở Trình duyệt (F12 > Console tab) ở một tài khoản **Customer A** (không phải là Admin).
2. Thử gõ thủ công lệnh sau vào console để "hack" tham gia vào đơn hàng mang ID = 999 (đơn hàng của người khác):
   ```javascript
   window.ecommerceHub.invoke("JoinOrderGroup", 999).catch(err => console.error(err));
   ```
3. **Kết quả mong đợi:** Trong Console sẽ văng ra Exception `"Unauthorized to join this order group."` và Customer A sẽ không thể nhận bất cứ event nào của đơn hàng số 999.
