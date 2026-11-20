using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T>
using EcommercePlatform.Application.Common.Models; // Import PaginatedList<T>
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Domain.Entities; // Import Entity Product
using Mapster; // Import extension ProjectToType của Mapster
using MapsterMapper; // Import interface IMapper
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts; // Namespace của slice GetProducts

public sealed class GetProductsHandler( // Handler xử lý query lấy danh sách sản phẩm
    IRepository<Product> productRepository, // Inject repository của Product
    IMapper mapper) : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>> // Implement IRequestHandler trả về PaginatedList<ProductDto>
{
    public async Task<PaginatedList<ProductDto>> Handle( // Hàm xử lý chính của MediatR
        GetProductsQuery request, // Dữ liệu query đã qua GetProductsValidator
        CancellationToken cancellationToken) // Token hủy request nếu client ngắt kết nối
    {
        var query = productRepository.AsQueryable(asNoTracking: true); // Lấy IQueryable read-only (EF tự áp dụng HasQueryFilter !IsDeleted)

        if (request.CategoryId.HasValue) // Nếu client có lọc theo danh mục
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value); // Lọc các sản phẩm thuộc CategoryId tương ứng
        }

        if (request.MinPrice.HasValue) // Nếu client có lọc theo giá tối thiểu
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value); // Lọc sản phẩm có giá >= MinPrice
        }

        if (request.MaxPrice.HasValue) // Nếu client có lọc theo giá tối đa
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value); // Lọc sản phẩm có giá <= MaxPrice
        }

        var keyword = request.Search?.Trim(); // Cắt khoảng trắng thừa của từ khóa tìm kiếm (nếu có)
        if (!string.IsNullOrEmpty(keyword)) // Nếu từ khóa khác rỗng
        {
            query = query.Where(p => // Lọc sản phẩm khớp từ khóa theo tên, SKU hoặc mô tả
                p.Name.Contains(keyword) || // Khớp trong Name
                p.Sku.Contains(keyword) || // Khớp trong Sku
                p.Description.Contains(keyword)); // Khớp trong Description
        }

        var projected = query // Bắt đầu build câu truy vấn projection sang DTO
            .OrderByDescending(p => p.CreatedAt) // Sắp xếp sản phẩm mới nhất lên đầu trước khi Skip/Take
            .ProjectToType<ProductDto>(mapper.Config); // Map trực tiếp sang ProductDto ở SQL (không load thừa cột)

        return await PaginatedList<ProductDto>.CreateAsync(projected, request.Page, request.Size, cancellationToken); // Đếm tổng số bản ghi và cắt đúng trang (Skip/Take) dưới SQL
    }
}