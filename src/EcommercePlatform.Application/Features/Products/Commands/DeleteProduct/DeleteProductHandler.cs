using EcommercePlatform.Application.Common.Interfaces; // Import IRepository<T> và IUnitOfWork
using EcommercePlatform.Domain.Entities; // Import Entity Product
using EcommercePlatform.Domain.Exceptions; // Import NotFoundException
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Commands.DeleteProduct; // Namespace của slice DeleteProduct

public sealed class DeleteProductHandler( // Handler xử lý xóa mềm sản phẩm (Soft Delete)
    IRepository<Product> productRepository, // Inject repository của Product
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand> // Implement IRequestHandler không trả dữ liệu (204 NoContent)
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken) // Hàm xử lý chính
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken) // Tìm sản phẩm có tracking (EF tự lọc !IsDeleted)
            ?? throw new NotFoundException(nameof(Product), request.Id); // Ném 404 Not Found nếu sản phẩm không tồn tại hoặc đã bị xóa mềm trước đó

        product.SoftDelete(); // Gọi method Domain để gán IsDeleted = true, DeletedAt = UtcNow (không xóa vật lý khỏi DB)

        await unitOfWork.SaveChangesAsync(cancellationToken); // Commit xuống SQL Server (EF Change Tracker tự nhận diện thay đổi)
    }
}