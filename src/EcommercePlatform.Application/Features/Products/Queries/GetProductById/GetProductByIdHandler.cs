using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T>
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Domain.Entities; // Import Entity Product
using EcommercePlatform.Domain.Exceptions; // Import NotFoundException (trả HTTP 404 qua GlobalExceptionHandler)
using Mapster; // Import extension ProjectToType của Mapster
using MapsterMapper; // Import interface IMapper
using MediatR; // Import IRequestHandler
using Microsoft.EntityFrameworkCore; // Import FirstOrDefaultAsync của EF Core

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById; // Namespace của slice GetProductById

public sealed class GetProductByIdHandler( // Handler xử lý lấy chi tiết 1 sản phẩm theo Id
    IRepository<Product> productRepository, // Inject repository của Product
    IMapper mapper) : IRequestHandler<GetProductByIdQuery, ProductDto> // Implement IRequestHandler trả về ProductDto
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken) // Hàm xử lý chính của MediatR
    {
        var product = await productRepository // Truy vấn từ bảng Products
            .AsQueryable(asNoTracking: true) // Chỉ đọc không tracking (EF tự lọc !IsDeleted)
            .Where(p => p.Id == request.Id) // Lọc đúng sản phẩm có Id khớp request
            .ProjectToType<ProductDto>(mapper.Config) // Map thẳng sang ProductDto ở SQL (tự JOIN lấy CategoryName)
            .FirstOrDefaultAsync(cancellationToken) // Lấy bản ghi đầu tiên hoặc null nếu không có / đã soft-delete
            ?? throw new NotFoundException(nameof(Product), request.Id); // Ném 404 Not Found nếu không tồn tại

        return product; // Trả về ProductDto cho Endpoint
    }
}