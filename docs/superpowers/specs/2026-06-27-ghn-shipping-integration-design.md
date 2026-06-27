# Thiết kế Tích hợp Dịch vụ Vận chuyển Giao Hàng Nhanh (GHN)

Tài liệu thiết kế chi tiết (Design Specification) cho việc tích hợp cổng vận chuyển Giao Hàng Nhanh (GHN) vào hệ thống thương mại điện tử Fruitables.

---

## 1. Mục tiêu & Phạm vi
- **Tính toán phí ship tự động**: Thay thế hệ thống tính phí vận chuyển thủ công hoặc tính phí tĩnh bằng việc gọi trực tiếp API tính phí của GHN dựa trên địa chỉ nhận hàng của khách (Tỉnh/Huyện/Xã) và tổng trọng lượng sản phẩm.
- **Tự động tạo đơn vận chuyển**: Khi Admin xác nhận đơn hàng (chuyển trạng thái sang `Processing`), hệ thống tự động đẩy đơn qua API GHN để lấy mã vận đơn và lập lịch lấy hàng.
- **Đồng bộ trạng thái giao hàng**: Tích hợp Webhook của GHN để cập nhật trạng thái đơn hàng thời gian thực (Đang giao -> Đã giao / Giao thất bại) vào cơ sở dữ liệu hệ thống.

---

## 2. Kiến trúc & Luồng Nghiệp Vụ

### 2.1 Luồng Khách hàng Đặt hàng (Checkout Flow)
```
[Khách hàng] ──(Nhập địa chỉ)──> [Giao diện Checkout] 
                                         │
                                   (Gọi API GHN)
                                         ▼
[Khách hàng] <──(Hiển thị phí ship)── [Hệ thống tính phí ship]
```
1. Người dùng chọn Tỉnh/Thành phố, Quận/Huyện, Phường/Xã từ danh sách được đồng bộ từ GHN.
2. Hệ thống tính tổng trọng lượng của giỏ hàng (`TotalWeight`).
3. Gọi API tính phí vận chuyển của GHN để lấy số tiền thực tế.
4. Cộng tiền ship vào tổng hóa đơn và lưu thông tin địa chỉ kèm ID địa giới của GHN.

### 2.2 Luồng Duyệt đơn & Đẩy đơn sang GHN (Admin Approval Flow)
1. Đơn hàng mới ở trạng thái `Pending`.
2. Admin kiểm tra và bấm "Xác nhận đơn hàng" (Trạng thái chuyển thành `Processing`).
3. Hệ thống kích hoạt event hoặc gọi trực tiếp `GhnService.CreateOrderAsync` gửi thông tin đơn hàng tới hệ thống GHN.
4. GHN phản hồi mã vận đơn `order_code` (Ví dụ: `ED123456789`).
5. Hệ thống lưu `GhnOrderCode` vào thông tin đơn hàng, đồng thời ghi log lịch sử đơn hàng.

### 2.3 Luồng Đồng bộ Trạng thái (Webhook / Tracking Flow)
1. Shipper cập nhật trạng thái đơn hàng trên app của GHN (Ví dụ: Lấy hàng thành công, Đã giao hàng).
2. GHN gửi request Webhook (HTTP POST JSON) đến endpoint của Fruitables: `/api/v1/ghn/webhook`.
3. Hệ thống xác thực signature của GHN.
4. Cập nhật trạng thái đơn hàng tương ứng trong DB:
   - Trạng thái GHN `delivered` -> Cập nhật đơn hàng thành `Delivered` & trạng thái thanh toán thành `Paid`.
   - Trạng thái GHN `cancel` / `return` -> Cập nhật đơn hàng thành `Cancelled` và tự động hoàn số lượng sản phẩm vào kho.

---

## 3. Thiết kế Cơ sở Dữ liệu (Database Schema)

### 3.1 Cập nhật bảng `Address` (Địa chỉ nhận hàng)
Thêm các cột sau vào bảng `Address` để liên kết với địa giới hành chính của GHN:
- `GhnProvinceId` (int, nullable): ID Tỉnh/Thành của GHN.
- `GhnDistrictId` (int, nullable): ID Quận/Huyện của GHN.
- `GhnWardCode` (string, max 20, nullable): Mã Phường/Xã của GHN.

### 3.2 Cập nhật bảng `Order` (Đơn hàng)
Thêm các trường phục vụ thông tin vận đơn:
- `GhnOrderCode` (string, max 50, nullable): Mã vận đơn do GHN cấp.
- `TotalWeight` (int, default 500): Tổng trọng lượng đơn hàng (tính bằng gram). Cần cập nhật để mặc định là 500g nếu sản phẩm không khai báo trọng lượng.

---

## 4. Thiết kế API Tích hợp (GHN API Contracts)

### 4.1 Header bắt buộc cho mọi API GHN
- `Token`: Token kết nối API (lấy từ cấu hình hệ thống).
- `ShopId`: ID của Shop đăng ký trên GHN (chỉ yêu cầu khi tính phí và tạo đơn).

### 4.2 Các Endpoint của GHN cần gọi

#### A. Lấy danh sách Tỉnh/Thành
- **Endpoint**: `GET https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/province`

#### B. Lấy danh sách Quận/Huyện
- **Endpoint**: `POST https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/district`
- **Request Body**:
  ```json
  {
    "province_id": 201
  }
  ```

#### C. Lấy danh sách Phường/Xã
- **Endpoint**: `POST https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/ward`
- **Request Body**:
  ```json
  {
    "district_id": 1442
  }
  ```

#### D. Tính phí vận chuyển (Calculate Fee)
- **Endpoint**: `POST https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee`
- **Request Body**:
  ```json
  {
    "from_district_id": 1442, // Quận của shop gửi hàng
    "to_district_id": 1444,   // Quận của khách nhận hàng
    "to_ward_code": "20314",  // Xã của khách nhận hàng
    "height": 10,             // Kích thước (cm)
    "length": 15,
    "width": 10,
    "weight": 500,            // Trọng lượng (gram)
    "service_id": 53320       // Mã gói dịch vụ chuẩn của GHN
  }
  ```

#### E. Tạo đơn vận chuyển (Create Shipping Order)
- **Endpoint**: `POST https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/create`
- **Request Body**:
  ```json
  {
    "payment_type_id": 1, // 1: Người bán trả phí, 2: Người mua trả phí
    "note": "Giao hàng giờ hành chính",
    "required_note": "CHOXEMHANGKHONGTHU", // Ghi chú xem hàng
    "to_name": "Nguyễn Văn A",
    "to_phone": "0901234567",
    "to_address": "123 Lê Lợi",
    "to_ward_code": "20314",
    "to_district_id": 1444,
    "weight": 500,
    "length": 15,
    "width": 10,
    "height": 10,
    "cod_amount": 450000, // Số tiền thu hộ COD (nếu chưa thanh toán)
    "items": [
      {
        "name": "Rau cải xanh",
        "code": "RAU-CAI",
        "quantity": 2,
        "price": 20000
      }
    ]
  }
  ```

---

## 5. Thiết kế Cấu trúc Mã Nguồn & Interface

### 5.1 GhnSettings (Cấu hình hệ thống)
```csharp
public class GhnSettings
{
    public string ApiUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/";
    public string ApiToken { get; set; } = "";
    public int ShopId { get; set; }
    public int FromDistrictId { get; set; } // Địa chỉ kho shop gửi hàng
}
```

### 5.2 IGhnService Interface
```csharp
using System.Threading.Tasks;
using System.Collections.Generic;
using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

public interface IGhnService
{
    Task<List<GhnProvince>> GetProvincesAsync();
    Task<List<GhnDistrict>> GetDistrictsAsync(int provinceId);
    Task<List<GhnWard>> GetWardsAsync(int districtId);
    Task<decimal> CalculateShippingFeeAsync(int toDistrictId, string toWardCode, int weight);
    Task<string> CreateShippingOrderAsync(Order order);
    Task<bool> CancelShippingOrderAsync(string ghnOrderCode);
}
```

---

## 6. Kế Hoạch Kiểm Thử (Verification Plan)
- **Kiểm thử đơn vị (Unit Test)**:
  - Viết Unit Test giả lập (Mock HttpClient) phản hồi của GHN API khi gọi tính phí và tạo đơn hàng.
  - Test trường hợp API GHN trả về lỗi (Ví dụ: địa chỉ không tồn tại, quá tải trọng) xem hệ thống xử lý ngoại lệ ra sao.
- **Kiểm thử tích hợp (Integration Test)**:
  - Chạy thử nghiệm trên môi trường Sandbox của GHN.
  - Kiểm tra giao diện chọn Tỉnh/Huyện/Xã tại trang Checkout có load đúng dữ liệu động từ GHN không.
  - Duyệt đơn trong trang quản trị Admin và xác nhận mã vận đơn `GhnOrderCode` được sinh ra chính xác.
