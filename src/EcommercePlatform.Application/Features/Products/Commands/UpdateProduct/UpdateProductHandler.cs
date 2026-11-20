using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T> và IUnitOfWork
using EcommercePlatform.Domain.Entities; // Import Entity Product và Category
using EcommercePlatform.Domain.Exceptions; // Import NotFoundException
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Commands.UpdateProduct; // Namespace của slice UpdateProduct

public sealed class UpdateProductHandler( // Handler xử lý cập nhật sản phẩm
    IRepository<Product> productRepository, // Inject repository của Product
    IRepository<Category> categoryRepository, // Inject repository của Category để kiểm tra danh mục tồn tại
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand> // Implement IRequestHandler không trả dữ liệu (204 NoContent)
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken) // Hàm xử lý chính
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken) // Tìm sản phẩm có tracking để EF theo dõi thay đổi (EF tự lọc !IsDeleted)
            ?? throw new NotFoundException(nameof(Product), request.Id); // Ném 404 Not Found nếu không tìm thấy hoặc đã bị xóa mềm

        if (request.CategoryId.HasValue) // Nếu có cập nhật sang danh mục khác
        {
            var categoryExists = await categoryRepository.AnyAsync( // Kiểm tra danh mục tồn tại (và chưa bị soft-delete)
                c => c.Id == request.CategoryId.Value, // So khớp Id danh mục
                cancellationToken); // Truyền cancellationToken

            if (!categoryExists) // Nếu danh mục không tồn tại
            {
                throw new NotFoundException(nameof(Category), request.CategoryId.Value); // Ném 404 Not Found
            }
        }

        product.UpdateDetails(request.Name, request.Description, request.Price, request.CategoryId); // Gọi method Domain để validate invariant và cập nhật trạng thái nội tại

        await unitOfWork.SaveChangesAsync(cancellationToken); // Commit thay đổi xuống SQL Server (EF Change Tracker tự nhận diện thay đổi)
    }
}