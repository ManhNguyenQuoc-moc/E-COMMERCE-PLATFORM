using MediatR; // Import IRequest của MediatR

namespace EcommercePlatform.Application.Features.Products.Commands.CreateProduct; // Namespace của slice CreateProduct

public sealed record CreateProductCommand( // Command tạo mới sản phẩm (chỉ Admin được gọi ở tầng Api)
    string Name, // Tên sản phẩm
    string Description, // Mô tả sản phẩm
    string Sku, // Mã SKU định danh duy nhất
    decimal Price, // Giá bán sản phẩm
    int StockQuantity, // Số lượng tồn kho ban đầu
    Guid? CategoryId = null) : IRequest<Guid>; // Id danh mục (tùy chọn, đặt cuối để tương thích code gọi cũ) và trả về Guid Id vừa tạo