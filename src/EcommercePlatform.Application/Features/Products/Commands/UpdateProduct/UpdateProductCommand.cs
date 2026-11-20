using MediatR; // Import IRequest của MediatR

namespace EcommercePlatform.Application.Features.Products.Commands.UpdateProduct; // Namespace của slice UpdateProduct

public sealed record UpdateProductCommand( // Command cập nhật thông tin sản phẩm (chỉ Admin được gọi ở tầng Api)
    Guid Id, // Id sản phẩm cần cập nhật
    string Name, // Tên sản phẩm mới
    string Description, // Mô tả sản phẩm mới
    decimal Price, // Giá bán mới
    Guid? CategoryId = null) : IRequest; // Id danh mục mới (tùy chọn, đặt cuối để tương thích code gọi cũ)