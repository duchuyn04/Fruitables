# Yêu cầu: Bỏ logic nhập địa chỉ thủ công ở trang Checkout

Mục tiêu: Đơn giản hóa trang thanh toán bằng cách yêu cầu người dùng phải chọn một địa chỉ giao hàng đã được lưu từ trước (ở trang `/Address`), thay vì cho phép nhập thông tin địa chỉ mới trực tiếp tại trang Checkout.

## User Review Required

> [!WARNING]
> Thay đổi này sẽ ảnh hưởng đến luồng thanh toán hiện tại. Người dùng CHƯA có địa chỉ nào sẽ không thể thanh toán ngay lập tức. Hệ thống sẽ hiển thị một thông báo và nút yêu cầu họ bấm vào để chuyển sang trang "Quản lý địa chỉ" thêm địa chỉ trước. Bạn xác nhận muốn thực hiện thay đổi này chứ?

## Proposed Changes

### Views/Checkout/Index.cshtml
- Xóa hoàn toàn khối form cho phép nhập địa chỉ mới (Họ tên, Số điện thoại, Số nhà, Tỉnh/Thành/Phường, tuỳ chọn lưu địa chỉ).
- Sửa lại khối chọn địa chỉ:
  - Bỏ tuỳ chọn `<option value="new">+ Thêm địa chỉ mới</option>`.
  - Nếu `savedAddresses` rỗng (người dùng chưa có địa chỉ): Hiển thị cảnh báo yêu cầu thêm địa chỉ và kèm theo một nút (button/link) trỏ tới `/Address` để thêm địa chỉ.
  - Xóa các script JavaScript hỗ trợ load Tỉnh/Thành phố ở cuối file (vì form nhập địa chỉ mới không còn tồn tại ở trang này).

### ViewModels/CheckoutViewModel.cs
- Xóa bỏ hoặc vô hiệu hóa validation (`[Required]`) của các trường địa chỉ (FirstName, Mobile, StreetAddress, ProvinceCode, v.v.).
- Đặt `SelectedAddressId` bắt buộc (`[Required(ErrorMessage = "Vui lòng chọn địa chỉ giao hàng")]`).

### Controllers/CheckoutController.cs
- Tại hàm `PlaceOrder`:
  - Kiểm tra `SelectedAddressId`, nếu không có (null) thì trả về lỗi yêu cầu người dùng chọn/thêm địa chỉ.
  - Vì chúng ta sẽ không còn nhận dữ liệu địa chỉ mới, nên các logic liên quan đến việc "nếu user nhập địa chỉ mới thì lưu vào database" sẽ bị loại bỏ hoặc bỏ qua. Mọi thông tin địa chỉ đơn hàng sẽ lấy hoàn toàn từ `SelectedAddressId` có sẵn trong Database.

## Verification Plan
### Manual Verification
- Chạy Playwright/Browser để kiểm tra trang `/Checkout`.
- Chắc chắn rằng bảng nhập địa chỉ đã biến mất.
- Đăng nhập tài khoản chưa có địa chỉ: Kiểm tra thông báo yêu cầu thêm địa chỉ hiển thị đúng.
- Đăng nhập tài khoản có địa chỉ: Có thể bấm đặt hàng thành công.
