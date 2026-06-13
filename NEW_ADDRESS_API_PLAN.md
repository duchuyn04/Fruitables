# Kế hoạch tích hợp API Địa chỉ mới (Cas AddressKit)

**Mục tiêu:** Thay thế API địa chỉ 3 cấp cũ (`provinces.open-api.vn`) bằng API địa chỉ 2 cấp mới nhất của Cục Thống Kê (`addresskit.cas.so`), giảm từ (Tỉnh/Thành - Quận/Huyện - Phường/Xã) xuống còn (Tỉnh/Thành - Phường/Xã).

## User Review Required
> [!WARNING]
> Thay đổi này có tác động rất lớn (Breaking Change) đến toàn bộ hệ thống vì nó thay đổi trực tiếp cấu trúc địa chỉ từ 3 cấp (Tỉnh - Huyện - Xã) xuống 2 cấp (Tỉnh - Xã). Toàn bộ Database, Model, và UI cần được cấu trúc lại. Cần chạy một script migration để gọi API `/convert` chuyển đổi dữ liệu địa chỉ cũ sang định dạng mới.

## Proposed Changes

### 1. Cập nhật `Program.cs`
- Đổi cấu hình `BaseAddress` của `HttpClient` tiêm vào `IVietnamAddressService` từ `https://provinces.open-api.vn/` thành `https://production.cas.so/address-kit/`.

### 2. Cập nhật Interface & Service (`IVietnamAddressService.cs` & `VietnamAddressService.cs`)
- Giữ hàm `GetProvincesAsync`, gọi API `GET /latest/provinces`.
- **[DELETE]** Xóa hàm `GetDistrictsByProvinceAsync`.
- **[DELETE]** Xóa hàm `GetWardsByDistrictAsync`.
- **[NEW]** Thêm hàm `GetCommunesByProvinceAsync(string provinceId)`, gọi API `GET /latest/provinces/{provinceID}/communes`.
- Bổ sung tham số `effectiveDate = "latest"` làm tham số mặc định khi gọi các API.

### 3. Cập nhật Database Model (`Models/Address.cs`, `Models/Order.cs`)
- **[DELETE]** Bỏ các trường: `DistrictCode`, `DistrictName`.
- Cập nhật các trường `WardCode`, `WardName` thành `CommuneCode`, `CommuneName` để phù hợp với tài liệu mới (hoặc giữ nguyên tên trường tùy quy ước, nhưng chỉ map dữ liệu 2 cấp).
- *Lưu ý: Cần tạo Migration Entity Framework mới để update DB Schema.*

### 4. Cập nhật ViewModels (`AddressViewModel`, `CheckoutViewModel`...)
- Loại bỏ các property `DistrictCode`, `DistrictName`.

### 5. Cập nhật Controller (`AddressApiController`, `UserAddressController`)
- Sửa lại API nội bộ `/api/address/districts` và `/api/address/wards`.
- Rút gọn luồng: `/api/address/provinces` -> `/api/address/communes` (trả về danh sách Xã theo Tỉnh).
- Bỏ bước truyền/kiểm tra dữ liệu District khi user thêm/sửa địa chỉ.

### 6. Cập nhật UI & JavaScript (`wwwroot/js/addressHandler.js` & Các View)
- Mở các View: `Areas/Profile/Views/UserAddress/Create.cshtml`, `Edit.cshtml`.
- **[DELETE]** Xóa thẻ `<select>` của phần Quận/Huyện.
- Sửa file `addressHandler.js`: Xóa logic lắng nghe sự kiện `onProvinceChange` gọi `District` rồi mới gọi `Ward`. Giờ đây `onProvinceChange` sẽ trực tiếp gọi API lấy danh sách `Commune` (Phường/Xã) và đổ vào `wardSelect` (hoặc `communeSelect`).

### 7. Data Migration
- Viết một tool/script nhỏ chạy một lần duy nhất để truy vấn các dòng Address cũ trong DB (có đủ Tỉnh, Huyện, Xã).
- Gọi `POST /convert` của AddressKit để lấy mapping định dạng 2 cấp mới.
- Lưu lại vào DB.

## Verification Plan
1. Xác minh hệ thống gọi thành công API `https://production.cas.so/address-kit/latest/provinces` lấy được toàn bộ Tỉnh/Thành phố hiện hành.
2. Tại màn hình Thêm địa chỉ mới, chọn "Hà Nội", giao diện ngay lập tức xổ ra tất cả Xã/Phường trực thuộc Hà Nội mà không có ô Quận/Huyện nào.
3. Submit thử form địa chỉ mới và đảm bảo nó lưu được vào DB không bị lỗi Validation liên quan đến Quận/Huyện.
4. Chạy tool migration và kiểm tra 100% user cũ không bị lỗi mất địa chỉ khi checkout đơn hàng.
