using EcommercePlatform.Application.Common.Models; // Import PaginatedList<T>
using EcommercePlatform.Application.Features.Products.Commands.CreateProduct; // Import CreateProductCommand
using EcommercePlatform.Application.Features.Products.Commands.DeleteProduct; // Import DeleteProductCommand
using EcommercePlatform.Application.Features.Products.Commands.UpdateProduct; // Import UpdateProductCommand
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Application.Features.Products.Queries.GetProductById; // Import GetProductByIdQuery
using EcommercePlatform.Application.Features.Products.Queries.GetProducts;
using EcommercePlatform.Application.Features.Products.Commands.PatchProduct; // Import PatchProductCommand
using MediatR; // Import ISender của MediatR

namespace EcommercePlatform.Api.Endpoints; // Namespace chứa các Minimal API endpoints

public static class ProductEndpoints // Static class đăng ký route cho Product Catalog
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app) // Extension method gọi từ Program.cs
    {
        var group = app.MapGroup("/api/v1/products") // Khai báo route prefix chuẩn v1 cho Products
            .WithTags("Products"); // Gom nhóm trên Swagger UI dưới tag "Products"

        group.MapGet("/", async ( // Endpoint 2.1: Lấy danh sách sản phẩm (Pagination + Filtering)
                [AsParameters] GetProductsQuery query, // Tự bind query string: page, size, categoryId, search, minPrice, maxPrice
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) => // Token hủy request
            {
                var result = await sender.Send(query, cancellationToken); // Gửi query qua MediatR (chạy GetProductsValidator -> GetProductsHandler)
                return TypedResults.Ok(result); // Trả 200 OK kèm PaginatedList<ProductDto>
            })
            .WithName("GetProducts") // Tên endpoint cho OpenAPI
            .AllowAnonymous() // Cho phép khách vãng lai xem danh sách sản phẩm
            .Produces<PaginatedList<ProductDto>>(StatusCodes.Status200OK) // Khai báo response 200 trên Swagger
            .ProducesValidationProblem(); // Khai báo response 400 nếu page/size/minPrice/maxPrice không hợp lệ

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => // Endpoint 2.2: Xem chi tiết 1 sản phẩm
            {
                var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken); // Gửi query lấy chi tiết sản phẩm theo Id
                return TypedResults.Ok(product); // Trả 200 OK kèm ProductDto
            })
            .WithName("GetProductById") // Tên endpoint cho OpenAPI
            .AllowAnonymous() // Cho phép khách vãng lai xem chi tiết sản phẩm
            .Produces<ProductDto>(StatusCodes.Status200OK) // Khai báo response 200 trên Swagger
            .ProducesValidationProblem() // Khai báo response 400 nếu Id là Guid.Empty
            .ProducesProblem(StatusCodes.Status404NotFound); // Khai báo response 404 nếu không tìm thấy hoặc đã soft-delete

        group.MapPost("/", async (CreateProductCommand command, ISender sender, CancellationToken cancellationToken) => // Endpoint 2.3 (Admin): Tạo mới sản phẩm
            {
                var productId = await sender.Send(command, cancellationToken); // Gửi command tạo sản phẩm qua MediatR
                return TypedResults.Created($"/api/v1/products/{productId}", new { Id = productId }); // Trả 201 Created kèm Location header và Id mới
            })
            .WithName("CreateProduct") // Tên endpoint cho OpenAPI
            .RequireAuthorization("AdminOnly") // Chỉ cho phép Role Admin truy cập (Step 0 - Bước 1)
            .Produces(StatusCodes.Status201Created) // Khai báo response 201 trên Swagger
            .ProducesValidationProblem() // Khai báo response 400 nếu dữ liệu đầu vào hoặc SKU trùng không hợp lệ
            .ProducesProblem(StatusCodes.Status401Unauthorized) // Khai báo response 401 nếu chưa đăng nhập
            .ProducesProblem(StatusCodes.Status403Forbidden) // Khai báo response 403 nếu không phải Admin
            .ProducesProblem(StatusCodes.Status404NotFound) // Khai báo response 404 nếu CategoryId truyền vào không tồn tại
            .ProducesProblem(StatusCodes.Status409Conflict); // Khai báo response 409 nếu xảy ra race condition trùng SKU khi tạo đồng thời

        group.MapPut("/{id:guid}", async ( // Endpoint 2.3 (Admin): Cập nhật thông tin sản phẩm
                Guid id, // Lấy Id sản phẩm từ route URL
                UpdateProductRequest request, // Đọc body JSON từ request
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) => // Token hủy request
            {
                await sender.Send( // Gửi UpdateProductCommand qua MediatR
                    new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.CategoryId), // Truyền kèm CategoryId mới (nếu có)
                    cancellationToken); // Truyền cancellationToken

                return TypedResults.NoContent(); // Trả 204 No Content khi cập nhật thành công
            })
            .WithName("UpdateProduct") // Tên endpoint cho OpenAPI
            .RequireAuthorization("AdminOnly") // Chỉ cho phép Role Admin truy cập
            .Produces(StatusCodes.Status204NoContent) // Khai báo response 204 trên Swagger
            .ProducesValidationProblem() // Khai báo response 400 nếu dữ liệu không hợp lệ
            .ProducesProblem(StatusCodes.Status401Unauthorized) // Khai báo response 401 nếu chưa đăng nhập
            .ProducesProblem(StatusCodes.Status403Forbidden) // Khai báo response 403 nếu không phải Admin
            .ProducesProblem(StatusCodes.Status404NotFound); // Khai báo response 404 nếu Product hoặc Category không tồn tại
        group.MapPatch("/{id:guid}", async ( // Endpoint 2.3 (Admin): Cập nhật từng phần thông tin sản phẩm
                Guid id, // Lấy Id sản phẩm từ route URL
                PatchProductRequest request, // Đọc body JSON (chỉ chứa các field cần sửa)
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) => // Token hủy request
            {
                await sender.Send( // Gửi PatchProductCommand qua MediatR
                    new PatchProductCommand(id, request.Name, request.Description, request.Price, request.CategoryId), // Truyền các field nullable từ request
                    cancellationToken); // Truyền cancellationToken
                return TypedResults.NoContent(); // Trả 204 No Content khi cập nhật thành công
            })
            .WithName("PatchProduct") // Tên endpoint cho OpenAPI
            .RequireAuthorization("AdminOnly") // Chỉ cho phép Role Admin truy cập giống PUT
            .Produces(StatusCodes.Status204NoContent) // Khai báo response 204 trên Swagger
            .ProducesValidationProblem() // Khai báo response 400 nếu input không hợp lệ hoặc không truyền field nào
            .ProducesProblem(StatusCodes.Status401Unauthorized) // Khai báo response 401 nếu chưa đăng nhập
            .ProducesProblem(StatusCodes.Status403Forbidden) // Khai báo response 403 nếu không phải Admin
            .ProducesProblem(StatusCodes.Status404NotFound); // Khai báo response 404 nếu Product hoặc Category không tồn tại
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => // Endpoint 2.3 (Admin): Xóa mềm sản phẩm (Soft Delete)
            {
                await sender.Send(new DeleteProductCommand(id), cancellationToken); // Gửi DeleteProductCommand (Handler sẽ gọi product.SoftDelete())
                return TypedResults.NoContent(); // Trả 204 No Content khi xóa mềm thành công
            })
            .WithName("DeleteProduct") // Tên endpoint cho OpenAPI
            .RequireAuthorization("AdminOnly") // Chỉ cho phép Role Admin truy cập
            .Produces(StatusCodes.Status204NoContent) // Khai báo response 204 trên Swagger
            .ProducesValidationProblem() // Khai báo response 400 nếu Id là Guid.Empty
            .ProducesProblem(StatusCodes.Status401Unauthorized) // Khai báo response 401 nếu chưa đăng nhập
            .ProducesProblem(StatusCodes.Status403Forbidden) // Khai báo response 403 nếu không phải Admin
            .ProducesProblem(StatusCodes.Status404NotFound); // Khai báo response 404 nếu sản phẩm không tồn tại

        return app; // Trả về IEndpointRouteBuilder để chain tiếp trong Program.cs
    }
}

public sealed record UpdateProductRequest( // Request DTO tách riêng khỏi route Id cho endpoint PUT
    string Name, // Tên sản phẩm mới
    string Description, // Mô tả sản phẩm mới
    decimal Price, // Giá bán mới
    Guid? CategoryId = null); // Id danh mục mới (tùy chọn)

public sealed record PatchProductRequest( // Request DTO cho endpoint PATCH (toàn bộ field đều nullable)
    string? Name = null, // Tên sản phẩm mới (tùy chọn)
    string? Description = null, // Mô tả sản phẩm mới (tùy chọn)
    decimal? Price = null, // Giá bán mới (tùy chọn)
    Guid? CategoryId = null); // Id danh mục mới (tùy chọn)