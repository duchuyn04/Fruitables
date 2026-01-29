# Fruitables - Hệ Thống Thương Mại Điện Tử Rau Củ Quả

## Giới Thiệu

Fruitables là một hệ thống thương mại điện tử chuyên về bán rau củ quả tươi sạch, được xây dựng trên nền tảng ASP.NET Core 8.0 với kiến trúc MVC. Hệ thống cung cấp đầy đủ các tính năng cho cả khách hàng và quản trị viên, từ mua sắm trực tuyến, quản lý đơn hàng đến thống kê doanh thu chi tiết.

## Công Nghệ Sử Dụng

### Backend
- **Framework**: ASP.NET Core 8.0 (MVC Pattern)
- **Database**: SQL Server với Entity Framework Core 8.0
- **Authentication**: 
  - Cookie-based Authentication
  - Google OAuth 2.0
  - BCrypt.Net cho mã hóa mật khẩu
- **ORM**: Entity Framework Core với Code-First Migrations
- **Dependency Injection**: Built-in ASP.NET Core DI Container

### Frontend
- **Template Engine**: Razor Views (.cshtml)
- **CSS Framework**: Bootstrap 5
- **JavaScript**: Vanilla JS, jQuery
- **UI Components**: Custom components với ViewComponents

### Testing
- **Framework**: xUnit
- **Property-Based Testing**: FsCheck
- **Mocking**: Moq
- **Coverage**: Hơn 80 test files với unit tests và property tests

### Libraries & Tools
- **ClosedXML**: Xuất báo cáo Excel
- **Memory Cache**: Caching dữ liệu thường xuyên truy cập
- **HttpClient**: Tích hợp API địa chỉ Việt Nam

## Kiến Trúc Hệ Thống

### Layered Architecture

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│  (Controllers, Views, ViewComponents)   │
├─────────────────────────────────────────┤
│          Service Layer                  │
│    (Business Logic Services)            │
├─────────────────────────────────────────┤
│        Repository Layer                 │
│   (Data Access, Unit of Work)           │
├─────────────────────────────────────────┤
│          Data Layer                     │
│  (Entity Framework, SQL Server)         │
└─────────────────────────────────────────┘
```

### Design Patterns
- **Repository Pattern**: Trừu tượng hóa data access
- **Unit of Work Pattern**: Quản lý transactions
- **Dependency Injection**: Loose coupling giữa các components
- **Service Layer Pattern**: Tách biệt business logic
- **ViewComponent Pattern**: Tái sử dụng UI components

## Cấu Trúc Dự Án
```
Fruitables/
│
├── Areas/
│   └── Admin/                    # Admin Panel (Dashboard, Quản lý)
│       ├── Controllers/          # Admin controllers
│       └── Views/                # Admin views
│
├── Controllers/                  # Public controllers
├── Services/                     # Business logic services
│   └── Interfaces/               # Service interfaces
│
├── Repositories/                 # Data access layer
│   └── Interfaces/               # Repository interfaces
│
├── Models/                       # Domain models & entities
├── ViewModels/                   # Data transfer objects cho views
├── Data/                         # DbContext & migrations
├── Helpers/                      # Utility classes
├── Constants/                    # Application constants
├── ViewComponents/               # Reusable UI components
├── Views/                        # Razor views
├── wwwroot/                      # Static files (CSS, JS, images)
└── Migrations/                   # EF Core migrations

Fruitables.Tests/                 # Test project
└── *Tests.cs                     # Unit & property tests
```
## Modules Chính

### 1. Module Xác Thực & Phân Quyền
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Đăng ký, đăng nhập với email/password
- Đăng nhập với Google OAuth
- Phân quyền: Customer, Admin, SuperAdmin
- Quản lý session và cookie authentication
- Khóa/mở khóa tài khoản người dùng

**Services**:
- `AuthenticationService`: Xác thực cơ bản
- `GoogleAuthService`: Tích hợp Google OAuth
- `UserAuthService`: Quản lý người dùng
- `UserManagementService`: Quản lý tài khoản (khóa/mở)

### 2. Module Quản Lý Sản Phẩm
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- CRUD sản phẩm với nhiều hình ảnh
- Quản lý danh mục (categories) với cấu trúc cây
- Quản lý biến thể sản phẩm (variants) với SKU
- Quản lý tags cho sản phẩm
- Lịch sử thay đổi sản phẩm (audit log)
- Tìm kiếm và lọc sản phẩm

**Services**:
- `ProductService`: Hiển thị sản phẩm cho khách hàng
- `ProductAdminService`: Quản lý sản phẩm (CRUD)
- `CategoryService`: Quản lý danh mục
- `ProductLogService`: Ghi log thay đổi
- `ImageUploadService`: Upload và quản lý hình ảnh

### 3. Module Giỏ Hàng & Thanh Toán
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Giỏ hàng với session storage
- Tính toán tự động phí vận chuyển theo vùng
- Quản lý địa chỉ giao hàng (tích hợp API địa chỉ VN)
- Snapshot địa chỉ và phí ship khi đặt hàng
- Áp dụng mã giảm giá (coupons)
- Xác nhận đơn hàng

**Services**:
- `CartService`: Quản lý giỏ hàng
- `ShippingService`: Tính phí vận chuyển
- `AddressService`: Quản lý địa chỉ
- `VietnamAddressService`: Tích hợp API địa chỉ VN
- `OrderService`: Xử lý đơn hàng

### 4. Module Quản Lý Đơn Hàng
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Theo dõi trạng thái đơn hàng (Pending → Processing → Shipped → Delivered)
- Quản lý trạng thái thanh toán (Unpaid → Paid → Refunded)
- Hủy đơn hàng với lý do và hoàn kho tự động
- Lịch sử thay đổi trạng thái với audit log
- Đính kèm file (attachments) cho đơn hàng
- Xử lý concurrency với RowVersion
- Lọc và tìm kiếm đơn hàng

**Services**:
- `OrderService`: Xử lý đơn hàng khách hàng
- `OrderAdminService`: Quản lý đơn hàng (Admin)
- `OrderHistoryService`: Lịch sử đơn hàng
- `OrderLogService`: Audit logging

**Models**:
- `Order`: Thông tin đơn hàng
- `OrderStatusHistory`: Lịch sử trạng thái
- `OrderStatusAuditLog`: Chi tiết audit log
- `AuditLogAttachment`: File đính kèm

### 5. Module Thống Kê Doanh Thu
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Thống kê doanh thu thuần (Net Revenue)
- Lọc theo khoảng thời gian với presets
- Doanh thu theo danh mục sản phẩm
- Top sản phẩm bán chạy
- Biểu đồ xu hướng (daily/weekly/monthly)
- So sánh giữa các kỳ
- Xuất báo cáo Excel
- Thống kê đơn hàng bị hủy với lý do

**Services**:
- `RevenueStatisticsService`: Thống kê doanh thu
- `CancelledOrdersStatisticsService`: Thống kê đơn hủy
- `DashboardService`: Tổng quan dashboard

**ViewModels**:
- `RevenueViewModel`: Dữ liệu doanh thu
- `CancelledOrdersViewModel`: Dữ liệu đơn hủy

### 6. Module Quản Lý Người Dùng
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Xem danh sách khách hàng với phân trang
- Xem chi tiết khách hàng và lịch sử mua hàng
- Khóa/mở khóa tài khoản (Temporary/Permanent)
- Phân loại vi phạm và ghi lý do
- Audit log cho các hành động quản lý
- Phân quyền Admin/SuperAdmin
- Gửi email thông báo khóa/mở khóa

**Services**:
- `UserManagementService`: Quản lý người dùng
- `EmailService`: Gửi email thông báo

**Models**:
- `User`: Thông tin người dùng
- `UserAccountLog`: Lịch sử khóa/mở
- `LockType`: Enum loại khóa
- `ViolationTypes`: Enum loại vi phạm

### 7. Module Đánh Giá & Testimonials
**Trạng thái**: ✅ Hoàn thành (Cơ bản)

**Chức năng**:
- Khách hàng đánh giá sản phẩm (rating + comment)
- Hiển thị đánh giá trên trang sản phẩm
- Quản lý testimonials
- Che từ ngữ không phù hợp (word masking)

**Services**:
- `ReviewService`: Quản lý đánh giá
- `TestimonialService`: Quản lý testimonials
- `WordMaskingService`: Lọc từ ngữ

### 8. Module Cấu Hình Hệ Thống
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Quản lý settings động (key-value)
- Cấu hình thông tin liên hệ
- Cấu hình social media links
- Cấu hình banner và features
- Cấu hình phí vận chuyển theo vùng
- Upload logo và hình ảnh

**Services**:
- `SettingsService`: Quản lý settings
- `ImageUploadService`: Upload hình ảnh

**Models**:
- `Setting`: Key-value settings
- `Shipping`: Cấu hình phí ship

### 9. Module Liên Hệ
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Form liên hệ từ khách hàng
- Lưu trữ tin nhắn liên hệ
- Hiển thị thông tin liên hệ và bản đồ

**Services**:
- `ContactService`: Xử lý liên hệ

**Models**:
- `ContactMessage`: Tin nhắn liên hệ

### 10. Module Profile
**Trạng thái**: ✅ Hoàn thành

**Chức năng**:
- Xem và chỉnh sửa thông tin cá nhân
- Upload avatar
- Quản lý địa chỉ giao hàng
- Xem lịch sử đơn hàng

**Services**:
- `ProfileService`: Quản lý profile

## Modules Sẽ Phát Triển Trong Tương Lai

### 1. Module Kiểm Duyệt Đánh Giá (Review Moderation)
**Trạng thái**: 📋 Đã lên kế hoạch

**Mô tả**: Module cho phép Admin duyệt, từ chối và xóa đánh giá của khách hàng, đảm bảo chất lượng nội dung.

**Chức năng dự kiến**:
- Xem danh sách đánh giá với lọc theo trạng thái (Pending/Approved/Rejected)
- Duyệt/từ chối đánh giá với lý do
- Duyệt/từ chối hàng loạt
- Xóa mềm (soft delete) đánh giá vi phạm
- Khôi phục đánh giá đã xóa (trong 30 ngày)
- Lịch sử kiểm duyệt (audit trail)
- Thống kê tổng quan đánh giá
- Phân quyền Moderator/Admin
- Gửi email thông báo khi từ chối
- Nâng cao word masking với severity levels

**Services dự kiến**:
- `ReviewModerationService`: Xử lý kiểm duyệt
- Nâng cấp `WordMaskingService`: Thêm severity levels

**Tài liệu**: `.kiro/specs/review-moderation/`

### 2. Module Quản Lý Kho & Tồn Kho
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Theo dõi tồn kho theo thời gian thực
- Cảnh báo hết hàng/sắp hết hàng
- Lịch sử nhập/xuất kho
- Quản lý nhà cung cấp
- Báo cáo tồn kho và dự báo nhu cầu
- Tự động đặt hàng khi dưới ngưỡng

### 3. Module Khuyến Mãi & Marketing
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Tạo và quản lý mã giảm giá (coupons)
- Flash sale với đếm ngược
- Chương trình tích điểm (loyalty points)
- Email marketing campaigns
- Push notifications
- Khuyến mãi theo nhóm khách hàng

### 4. Module Báo Cáo Nâng Cao
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Báo cáo lợi nhuận (revenue - cost)
- Phân tích hành vi khách hàng (RFM analysis)
- Dự báo doanh thu (forecasting)
- Báo cáo hiệu suất sản phẩm
- Dashboard tùy chỉnh
- Xuất báo cáo tự động theo lịch

### 5. Module Chat & Hỗ Trợ Khách Hàng
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Live chat với khách hàng
- Chatbot tự động trả lời
- Ticket system cho support
- FAQ management
- Rating hỗ trợ khách hàng

### 6. Module Giao Hàng & Vận Chuyển
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Tích hợp API đơn vị vận chuyển (GHN, GHTK, Viettel Post)
- Theo dõi đơn hàng real-time
- In phiếu giao hàng tự động
- Quản lý shipper nội bộ
- Tối ưu tuyến đường giao hàng

### 7. Module Mobile App API
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- RESTful API cho mobile app
- JWT authentication
- Push notification service
- API documentation với Swagger
- Rate limiting và security

### 8. Module Phân Tích & AI
**Trạng thái**: 💡 Ý tưởng

**Chức năng dự kiến**:
- Gợi ý sản phẩm (recommendation engine)
- Phân loại khách hàng tự động
- Phát hiện gian lận (fraud detection)
- Tối ưu giá (dynamic pricing)
- Sentiment analysis cho đánh giá

## Cài Đặt & Chạy Dự Án

### Yêu Cầu Hệ Thống
- .NET 8.0 SDK
- SQL Server 2019 hoặc mới hơn
- Visual Studio 2022 hoặc VS Code
- Node.js (cho frontend tooling - optional)

### Các Bước Cài Đặt

1. **Clone repository**

```bash
git clone <repository-url>
cd Fruitables
```


2. **Cấu hình Connection String**

Chỉnh sửa `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FruitablesDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. **Cấu hình Google OAuth (Optional)**

Thêm vào `appsettings.json` hoặc User Secrets:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

4. **Chạy Migrations**

```bash
cd Fruitables
dotnet ef database update
```


5. **Chạy ứng dụng**

```bash
dotnet run
```

6. **Truy cập ứng dụng**
- Frontend: `https://localhost:5001`
- Admin Panel: `https://localhost:5001/Admin`

### Tài Khoản Mặc Định

**Admin**:
- Email: `admin@fruitables.com`
- Password: `Admin@123`

**Super Admin**:
- Email: `superadmin@fruitables.com`
- Password: `Admin@123`

## Chạy Tests

```bash
cd Fruitables.Tests
dotnet test
```

Xem coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Cấu Trúc Database

### Bảng Chính
- `Users`: Người dùng (Customer/Admin/SuperAdmin)
- `Addresses`: Địa chỉ giao hàng
- `Categories`: Danh mục sản phẩm
- `Products`: Sản phẩm
- `ProductImages`: Hình ảnh sản phẩm
- `ProductVariants`: Biến thể sản phẩm
- `ProductTags`: Tags sản phẩm
- `Orders`: Đơn hàng
- `OrderItems`: Chi tiết đơn hàng
- `OrderStatusHistory`: Lịch sử trạng thái
- `OrderStatusAuditLog`: Audit log đơn hàng
- `Reviews`: Đánh giá sản phẩm
- `Carts`: Giỏ hàng
- `CartItems`: Sản phẩm trong giỏ
- `Settings`: Cấu hình hệ thống
- `ContactMessages`: Tin nhắn liên hệ
- `Testimonials`: Testimonials
- `UserAccountLogs`: Log khóa/mở tài khoản
- `ProductLogs`: Log thay đổi sản phẩm

## API Endpoints

### Public APIs
- `GET /`: Trang chủ
- `GET /Shop`: Danh sách sản phẩm
- `GET /Shop/Detail/{id}`: Chi tiết sản phẩm
- `GET /Cart`: Giỏ hàng
- `POST /Cart/Add`: Thêm vào giỏ
- `GET /Checkout`: Thanh toán
- `POST /Checkout/PlaceOrder`: Đặt hàng
- `GET /Account/Login`: Đăng nhập
- `POST /Account/Login`: Xử lý đăng nhập
- `GET /Account/Register`: Đăng ký
- `POST /Account/Register`: Xử lý đăng ký

### Admin APIs
- `GET /Admin/Dashboard`: Dashboard
- `GET /Admin/Product`: Quản lý sản phẩm
- `GET /Admin/Order`: Quản lý đơn hàng
- `GET /Admin/User`: Quản lý người dùng
- `GET /Admin/Revenue`: Thống kê doanh thu
- `GET /Admin/Settings`: Cấu hình hệ thống

### Address API
- `GET /api/address/provinces`: Danh sách tỉnh/thành
- `GET /api/address/districts/{provinceId}`: Danh sách quận/huyện
- `GET /api/address/wards/{districtId}`: Danh sách phường/xã

## License

Dự án này được phát triển cho mục đích học tập và thương mại.

**Phiên bản**: 1.0.0  
**Cập nhật lần cuối**: Tháng 1, 2026
