using EcommercePlatform.Domain.Enums; // Import enum ProductStatus từ tầng Domain

namespace EcommercePlatform.Application.Features.Products.DTOs; // Khai báo namespace cho DTO của Products

public sealed record ProductDto( // Record bất biến dùng làm dữ liệu trả về cho API Product
    Guid Id, // Khóa chính của sản phẩm
    Guid? CategoryId, // Id danh mục (nullable vì sản phẩm có thể chưa gán danh mục)
    string? CategoryName, // Tên danh mục đi kèm để client hiển thị trực tiếp
    string Name, // Tên sản phẩm
    string Description, // Mô tả chi tiết sản phẩm
    string Sku, // Mã SKU định danh duy nhất
    decimal Price, // Giá bán sản phẩm
    int StockQuantity, // Số lượng tồn kho hiện tại
    ProductStatus Status, // Trạng thái sản phẩm (Active / OutOfStock / ...)
    DateTimeOffset CreatedAt, // Thời điểm tạo bản ghi
    DateTimeOffset? UpdatedAt); // Thời điểm cập nhật gần nhất (nullable)