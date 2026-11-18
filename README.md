# E-Commerce Platform API

Backend REST API cho nền tảng thương mại điện tử, xây dựng trên **.NET 10 (C# 14)** theo mô hình **Clean Architecture** kết hợp **Minimal APIs** và **CQRS (MediatR)**.

---

## 1. Công nghệ sử dụng

- **Framework:** .NET 10 (`net10.0`), ASP.NET Core Minimal APIs
- **Kiến trúc:** Clean Architecture (4 tầng: `Domain`, `Application`, `Infrastructure`, `Api`)
- **CQRS & Validation:** MediatR, FluentValidation
- **ORM & Database:** Entity Framework Core 10 (Code-First), SQL Server 2022 (Docker)
- **Error Handling:** `IExceptionHandler` trả về chuẩn `ProblemDetails` (RFC 9457)
- **API Documentation:** Microsoft.AspNetCore.OpenApi, Swagger UI, Scalar

---

## 2. Cấu trúc Solution

```text
E-COMMERCE-PLATFORM/
├── EcommercePlatform.sln
├── docker-compose.yml
├── .env.example
└── src/
    ├── EcommercePlatform.Domain/          # Entities, Enums, ValueObjects, DomainExceptions
    ├── EcommercePlatform.Application/     # CQRS (Commands/Queries), Validators, Behaviors, Interfaces
    ├── EcommercePlatform.Infrastructure/  # AppDbContext, EF Configurations, Migrations, External Services
    └── EcommercePlatform.Api/             # Minimal API Endpoints, GlobalExceptionHandler, OpenAPI/Swagger
```

### Quy tắc phụ thuộc (Dependency Rule)
- `Domain`: Không tham chiếu project hay framework nào.
- `Application`: Chỉ tham chiếu `Domain`.
- `Infrastructure`: Chỉ tham chiếu `Application`.
- `Api`: Tham chiếu `Application` và `Infrastructure`.

---

## 3. Yêu cầu môi trường (Prerequisites)

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (để chạy SQL Server)
- *(Tuỳ chọn)* `dotnet-ef` CLI để chạy migration:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

## 4. Hướng dẫn chạy Local

### Bước 1: Tạo file cấu hình `.env`
Sao chép file mẫu `.env.example` thành `.env` ở thư mục gốc và chỉnh sửa mật khẩu/cấu hình nếu cần:

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

### Bước 3: Chạy EF Core Migrations (tạo bảng trong DB)
```powershell
dotnet ef migrations add InitialCreate --project src/EcommercePlatform.Infrastructure --startup-project src/EcommercePlatform.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/EcommercePlatform.Infrastructure --startup-project src/EcommercePlatform.Api
```

### Bước 4: Build & Run Backend API
```powershell
dotnet build EcommercePlatform.sln
dotnet run --project src/EcommercePlatform.Api
```

---

## 5. Tài liệu API (Development)

Sau khi ứng dụng khởi động, truy cập các đường dẫn sau trên trình duyệt:

- **Swagger UI:** [http://localhost:5185/swagger](http://localhost:5185/swagger) *(hoặc `https://localhost:7095/swagger`)*
- **Scalar UI:** [http://localhost:5185/scalar/v1](http://localhost:5185/scalar/v1)
- **OpenAPI JSON:** [http://localhost:5185/openapi/v1.json](http://localhost:5185/openapi/v1.json)

