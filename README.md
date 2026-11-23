# E-Commerce Platform API

Backend REST API cho nền tảng thương mại điện tử, xây dựng trên **.NET 10 (C# 14)** theo mô hình **Clean Architecture** kết hợp **Minimal APIs** và **CQRS (MediatR)**.

---

## 1. Công nghệ sử dụng

- **Framework:** .NET 10 (`net10.0`), ASP.NET Core Minimal APIs
- **Kiến trúc:** Clean Architecture (4 tầng: `Domain`, `Application`, `Infrastructure`, `Api`)
- **CQRS, Validation & Mapping:** MediatR, FluentValidation, Mapster
- **ORM & Database:** Entity Framework Core 10 (Code-First), SQL Server 2022 (Docker)
- **Authentication & Security:** JWT Bearer (Access + Refresh Token Rotation), BCrypt, RBAC (`AdminOnly`, `UserOrAdmin`), ASP.NET Core Rate Limiting, `IMemoryCache`
- **Error Handling:** `IExceptionHandler` trả về chuẩn `ProblemDetails` (RFC 9457)
- **API Documentation:** `Microsoft.AspNetCore.OpenApi`, Swagger UI
- **Testing:** xUnit, Moq, EF Core InMemory (`197` Unit Tests) & E2E JSON Test Cases (`41` cases)

---

## 2. Cấu trúc Solution

```text
E-COMMERCE-PLATFORM/
├── EcommercePlatform.sln
├── docker-compose.yml
├── .env.example
├── src/
│   ├── EcommercePlatform.Domain/          # Entities, Enums, DomainExceptions
│   ├── EcommercePlatform.Application/     # CQRS (Commands/Queries), Validators, Behaviors, Mappings, Interfaces
│   ├── EcommercePlatform.Infrastructure/  # AppDbContext, EF Configurations, Migrations, Repositories, JWT/BCrypt
│   └── EcommercePlatform.Api/             # Minimal API Endpoints, Middlewares, Rate Limiting, OpenAPI/Swagger
└── tests/
    ├── EcommercePlatform.UnitTests/       # Unit tests cho Domain, Application, Infrastructure (xUnit + Moq)
    └── e2e-api-cases/                     # Bộ 41 file JSON kiểm thử E2E (Happy & Bad cases) qua API
```

### Quy tắc phụ thuộc (Dependency Rule)
- `Domain`: Không tham chiếu project hay framework nào.
- `Application`: Chỉ tham chiếu `Domain`.
- `Infrastructure`: Chỉ tham chiếu `Application`.
- `Api`: Tham chiếu `Application` và `Infrastructure`.

---

## 3. Tính năng chính đã triển khai (Tasks 1–6)

1. **Authentication & RBAC (`/api/v1/auth/*`):** Đăng ký, đăng nhập (chống timing attack), Refresh Token Rotation, đăng xuất thu hồi token (kiểm tra chủ sở hữu), phân quyền `User` / `Admin`.
2. **Product Catalog (`/api/v1/products/*`):** Danh mục & sản phẩm, tìm kiếm, lọc theo khoảng giá/danh mục, phân trang, xóa mềm (`SoftDelete`), chuẩn hóa SKU duy nhất.
3. **Shopping Cart (`/api/v1/cart/*`):** Xem giỏ hàng, thêm/cập nhật/xóa mặt hàng (`quantity = 0`), kiểm tra tồn kho & trạng thái `Active`, đồng bộ chiết khấu hạng thành viên & coupon.
4. **Order & Checkout (`/api/v1/orders/*`):** Đặt hàng nguyên tử trong transaction, chống oversell bằng `RowVersion` Optimistic Concurrency, lịch sử đơn hàng phân trang.
5. **Promotion & Loyalty (`/api/v1/admin/coupons`, `/api/v1/admin/orders/{id}/status`):** Mã giảm giá (`PERCENTAGE` / `FIXED_AMOUNT`), Strict Order State Machine, hoàn kho & hoàn lượt coupon khi hủy đơn, tích điểm & nâng hạng (`Member`, `Silver`, `Gold`, `Diamond`).
6. **Product Reviews (`/api/v1/reviews`, `/api/v1/products/{id}/reviews`):** Đánh giá sản phẩm (chỉ cho đơn `Completed`, chống trùng lặp), thống kê điểm trung bình & danh sách đánh giá phân trang.

---

## 4. Yêu cầu môi trường (Prerequisites)

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (để chạy SQL Server)
- *(Tuỳ chọn)* `dotnet-ef` CLI để chạy migration:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

## 5. Hướng dẫn chạy Local

### Bước 1: Tạo file cấu hình `.env`
Sao chép file mẫu `.env.example` thành `.env` ở thư mục gốc và cập nhật các biến cần thiết (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `Cors__AllowedOrigins__0..2`):

```powershell
Copy-Item .env.example .env
```

### Bước 2: Khởi chạy Database (SQL Server via Docker)
```powershell
docker compose up -d
```
Kiểm tra container đang chạy và `healthy`:
```powershell
docker compose ps
```

### Bước 3: Áp dụng EF Core Migrations vào Database
```powershell
dotnet ef database update --project src/EcommercePlatform.Infrastructure --startup-project src/EcommercePlatform.Api
```

### Bước 4: Build & Run Backend API
```powershell
dotnet build EcommercePlatform.sln
dotnet run --project src/EcommercePlatform.Api
```

### Bước 5: Chạy Unit Tests
```powershell
dotnet test EcommercePlatform.sln
```

---

## 6. Tài liệu API (Development)

Sau khi ứng dụng khởi động ở môi trường `Development`, truy cập:

- **Swagger UI:** [http://localhost:5185/swagger](http://localhost:5185/swagger) *(hoặc `https://localhost:7095/swagger`)*
- **OpenAPI JSON:** [http://localhost:5185/openapi/v1.json](http://localhost:5185/openapi/v1.json)
