using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T> và IUnitOfWork
using EcommercePlatform.Domain.Entities; // Import Entity Product và Category
using EcommercePlatform.Domain.Exceptions; // Import NotFoundException
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct; // Namespace của slice PatchProduct

public sealed class PatchProductHandler( // Handler xử lý cập nhật từng phần sản phẩm
    IRepository<Product> productRepository, // Inject repository của Product
    IRepository<Category> categoryRepository, // Inject repository của Category để kiểm tra danh mục mới nếu có
    IUnitOfWork unitOfWork) : IRequestHandler<PatchProductCommand> // Implement IRequestHandler trả về 204 NoContent
{
    public async Task Handle(PatchProductCommand request, CancellationToken cancellationToken) // Hàm xử lý chính
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken) // Tìm sản phẩm có tracking (EF tự lọc !IsDeleted)
            ?? throw new NotFoundException(nameof(Product), request.Id); // Ném 404 Not Found nếu không tồn tại hoặc đã soft-delete

        if (request.CategoryId.HasValue) // Nếu client có truyền CategoryId mới
        {
            var categoryExists = await categoryRepository.AnyAsync( // Kiểm tra danh mục mới có tồn tại trong DB không
                c => c.Id == request.CategoryId.Value, // So khớp Id danh mục
                cancellationToken); // Truyền cancellationToken

            if (!categoryExists) // Nếu danh mục không tồn tại hoặc đã soft-delete
            {
                throw new NotFoundException(nameof(Category), request.CategoryId.Value); // Ném 404 Not Found
            }
        }

        product.UpdateDetails( // Tái sử dụng method Domain UpdateDetails để giữ nguyên mọi business invariant và cập nhật UpdatedAt
            request.Name ?? product.Name, // Nếu Name != null thì lấy Name mới, ngược lại giữ nguyên product.Name
            request.Description ?? product.Description, // Nếu Description != null thì lấy Description mới, ngược lại giữ nguyên
            request.Price ?? product.Price, // Nếu Price != null thì lấy Price mới, ngược lại giữ nguyên
            request.CategoryId ?? product.CategoryId); // Nếu CategoryId != null thì lấy CategoryId mới, ngược lại giữ nguyên

        await unitOfWork.SaveChangesAsync(cancellationToken); // Commit xuống SQL Server (EF Change Tracker tự nhận diện thay đổi)
    }
}