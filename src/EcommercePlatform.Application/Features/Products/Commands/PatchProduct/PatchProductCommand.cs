using MediatR; // Import IRequest của MediatR

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct; // Namespace của slice PatchProduct

public sealed record PatchProductCommand( // Command cập nhật từng phần sản phẩm (chỉ field != null mới cập nhật)
    Guid Id, // Id sản phẩm cần cập nhật
    string? Name = null, // Tên mới (null = giữ nguyên tên cũ)
    string? Description = null, // Mô tả mới (null = giữ nguyên mô tả cũ)
    decimal? Price = null, // Giá mới (null = giữ nguyên giá cũ)
    Guid? CategoryId = null) : IRequest; // Danh mục mới (null = giữ nguyên danh mục cũ; muốn gỡ về null thì dùng PUT)