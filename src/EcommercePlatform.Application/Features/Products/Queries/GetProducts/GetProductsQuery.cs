using EcommercePlatform.Application.Common.Models; // Import PaginatedList<T>
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using MediatR; // Import IRequest của MediatR

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts; // Namespace của slice GetProducts

public sealed record GetProductsQuery( // Query lấy danh sách sản phẩm có phân trang và bộ lọc
    int Page = 1, // Trang hiện tại, mặc định là trang 1
    int Size = 10, // Số phần tử mỗi trang, mặc định là 10
    Guid? CategoryId = null, // Lọc theo Id danh mục (tùy chọn)
    string? Search = null, // Lọc theo từ khóa tên/SKU/mô tả (tùy chọn)
    decimal? MinPrice = null, // Lọc giá tối thiểu (tùy chọn)
    decimal? MaxPrice = null) : IRequest<PaginatedList<ProductDto>>; // Trả về danh sách phân trang ProductDto