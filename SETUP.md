# 🚀 Hướng Dẫn Cài Đặt - Fruitables E-commerce

## 📋 Yêu Cầu Hệ Thống

Trước khi bắt đầu, bạn cần cài đặt các phần mềm sau:

- **.NET 8.0 SDK** - [Tải tại đây](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB hoặc bản đầy đủ) - [Tải SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)
- **Visual Studio 2022** hoặc **VS Code** - [Tải Visual Studio](https://visualstudio.microsoft.com/)
- **Git** - [Tải Git](https://git-scm.com/downloads)

---

## 🔧 Các Bước Cài Đặt

### Bước 1: Tải Mã Nguồn

Mở **Command Prompt** hoặc **Terminal** và chạy lệnh:

```bash
git clone https://github.com/duchuy19012004/Fruitables.git
cd Fruitables
```

### Bước 2: Cấu Hình Kết Nối Database

#### 2.1. Tạo File Cấu Hình

Sao chép file mẫu:

```bash
copy appsettings.example.json appsettings.json
```

#### 2.2. Chỉnh Sửa Connection String

Mở file `appsettings.json` và sửa phần connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TÊN_SERVER;Database=FruitablesDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

**Thay `TÊN_SERVER` bằng:**
- `(localdb)\\mssqllocaldb` - nếu dùng LocalDB (đi kèm Visual Studio)
- `.` hoặc `localhost` - nếu dùng SQL Server cài trên máy
- Tên server của bạn - nếu dùng SQL Server từ xa

**Ví dụ với LocalDB:**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FruitablesDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### Bước 3: Cài Đặt Các Package Cần Thiết

```bash
dotnet restore
```

Lệnh này sẽ tải về tất cả các thư viện cần thiết cho project.

### Bước 4: Tạo Database

Chạy lệnh sau để tạo database và các bảng:

```bash
dotnet ef database update
```

Lệnh này sẽ:
- Tạo database `FruitablesDb`
- Tạo tất cả các bảng (Users, Products, Orders, Categories, v.v.)
- Tạo hệ thống phân quyền RBAC

### Bước 5: Chạy Ứng Dụng

#### Cách 1: Dùng Command Line
```bash
dotnet run
```

#### Cách 2: Dùng Visual Studio
- Mở file `Fruitables.sln`
- Nhấn **F5** hoặc click nút **Run**

Ứng dụng sẽ chạy tại:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

---

## 🔐 Thiết Lập Hệ Thống Phân Quyền (RBAC)

### Cách 1: Chạy Script SQL (Khuyến Nghị)

1. Mở file `seed-rbac-data.sql` (nếu có trong project)
2. Mở **SQL Server Management Studio** (SSMS)
3. Kết nối đến database `FruitablesDb`
4. Chạy script để tạo:
   - 3 roles: Customer, Admin, SuperAdmin
   - 34 permissions
   - 3 tài khoản test

**Tài khoản mặc định sau khi chạy script:**
- **Customer**: customer@gmail.com / Password123!
- **Admin**: admin@gmail.com / Password123!
- **SuperAdmin**: superadmin@gmail.com / Password123!

### Cách 2: Qua Giao Diện Web

1. Đăng nhập với tài khoản admin
2. Truy cập: `http://localhost:5000/Admin/Diagnostics/Migration`
3. Click nút **"Run Migration"**
4. Đăng xuất và đăng nhập lại

---

## 📁 Cấu Trúc Project

```
Fruitables/
├── Areas/Admin/          # Trang quản trị
│   ├── Controllers/      # Controllers cho admin
│   └── Views/           # Giao diện admin
├── Controllers/         # Controllers công khai
├── Models/             # Models dữ liệu
├── Services/           # Business logic
├── Repositories/       # Truy xuất dữ liệu
├── ViewModels/         # View models
├── Views/              # Giao diện công khai
├── wwwroot/            # File tĩnh (CSS, JS, images)
└── Data/               # Database context
```

---

## 🎯 Các Tính Năng Chính

### Dành Cho Khách Hàng
- 🛒 Mua sắm trực tuyến với giỏ hàng
- 🔍 Tìm kiếm và lọc sản phẩm
- 📦 Theo dõi đơn hàng
- ⭐ Đánh giá sản phẩm
- 👤 Quản lý tài khoản cá nhân
- 📍 Quản lý địa chỉ giao hàng

### Dành Cho Admin
- 👥 **Quản lý người dùng**: Khóa/mở khóa tài khoản, phân quyền
- � **Quản lý đơn hàng**: Xử lý đơn hàng, cập nhật trạng thái
- 🏷️ **Quản lý sản phẩm**: Thêm/sửa/xóa sản phẩm, danh mục
- 💰 **Thống kê doanh thu**: Báo cáo chi tiết theo ngày/tháng/năm
- 🔐 **Hệ thống RBAC**: Phân quyền chi tiết theo chức năng
- ⭐ **Quản lý đánh giá**: Kiểm duyệt review sản phẩm
- 🚚 **Cấu hình vận chuyển**: Thiết lập phí ship

---

## 🛠️ Lệnh Hữu Ích Cho Developer

### Build Project
```bash
dotnet build
```

### Chạy Tests
```bash
dotnet test
```

### Tạo Migration Mới
```bash
dotnet ef migrations add TenMigration
```

### Cập Nhật Database
```bash
dotnet ef database update
```

### Xóa Database (Cẩn thận!)
```bash
dotnet ef database drop
```

---

## � Các File Cấu Hình

| File | Mô Tả | Commit vào Git? |
|------|-------|-----------------|
| `appsettings.json` | Cấu hình chính (chứa connection string) | ❌ KHÔNG |
| `appsettings.example.json` | File mẫu cho cấu hình | ✅ CÓ |
| `appsettings.Development.json` | Cấu hình cho môi trường dev | ❌ KHÔNG |

**⚠️ Quan trọng:** Không bao giờ commit file `appsettings.json` hoặc `appsettings.Development.json` vào Git vì chứa thông tin nhạy cảm!

---

## 🚨 Xử Lý Lỗi Thường Gặp

### Lỗi: Không Kết Nối Được Database

**Nguyên nhân:**
- SQL Server chưa chạy
- Connection string sai
- Không có quyền truy cập database

**Giải pháp:**
1. Kiểm tra SQL Server đã chạy chưa:
   - Mở **Services** (Windows + R → `services.msc`)
   - Tìm **SQL Server** và đảm bảo đang chạy
2. Kiểm tra lại connection string trong `appsettings.json`
3. Thử kết nối bằng SSMS để test

### Lỗi: Migration Thất Bại

**Giải pháp:**
1. Xóa database và chạy lại:
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```
2. Kiểm tra file migration có lỗi không
3. Xem log chi tiết trong console

### Lỗi: Không Có Quyền Truy Cập (403 Forbidden)

**Giải pháp:**
1. Truy cập: `http://localhost:5000/Admin/Diagnostics/Migration`
2. Chạy RBAC migration
3. Đăng xuất và đăng nhập lại
4. Kiểm tra user đã được gán role chưa

### Lỗi: Port 5000 Đã Được Sử Dụng

**Giải pháp:**
1. Đổi port trong `Properties/launchSettings.json`
2. Hoặc tắt ứng dụng đang dùng port 5000

---

## 💡 Hướng Dẫn Sử Dụng Nhanh

### Cho Người Dùng Mới

1. **Đăng ký tài khoản:**
   - Truy cập trang chủ
   - Click "Đăng ký"
   - Điền thông tin và xác nhận

2. **Mua hàng:**
   - Duyệt sản phẩm
   - Thêm vào giỏ hàng
   - Thanh toán và điền địa chỉ giao hàng

3. **Theo dõi đơn hàng:**
   - Vào "Tài khoản" → "Lịch sử đơn hàng"
   - Xem chi tiết và trạng thái đơn

### Cho Admin

1. **Đăng nhập admin:**
   - Truy cập: `http://localhost:5000/Admin`
   - Đăng nhập với tài khoản admin

2. **Quản lý sản phẩm:**
   - Vào "Products" → "Create"
   - Điền thông tin và upload ảnh
   - Lưu sản phẩm

3. **Xử lý đơn hàng:**
   - Vào "Orders"
   - Click vào đơn hàng cần xử lý
   - Cập nhật trạng thái

---

## 🔒 Lưu Ý Bảo Mật

- ✅ Luôn dùng HTTPS trong production
- ✅ Giữ `appsettings.json` an toàn, không commit vào Git
- ✅ Cập nhật dependencies thường xuyên
- ✅ Dùng mật khẩu mạnh cho tài khoản admin
- ✅ Đổi mật khẩu mặc định sau khi cài đặt
- ✅ Backup database định kỳ

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề:

1. **Kiểm tra trang Diagnostics:**
   - Truy cập: `http://localhost:5000/Admin/Diagnostics`
   - Xem trạng thái hệ thống

2. **Xem log lỗi:**
   - Kiểm tra console khi chạy `dotnet run`
   - Xem file log (nếu có)

3. **Liên hệ:**
   - Email: duchuy19012004@gmail.com
   - GitHub Issues: [Tạo issue mới](https://github.com/duchuy19012004/Fruitables/issues)

---

## 📚 Tài Liệu Thêm

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [SQL Server Documentation](https://docs.microsoft.com/sql)

---

## 📄 License

MIT License - Xem file LICENSE để biết thêm chi tiết.

---

**Chúc bạn sử dụng project thành công! 🎉**
