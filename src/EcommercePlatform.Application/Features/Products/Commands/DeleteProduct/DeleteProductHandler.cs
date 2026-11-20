using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandler(
    IRepository<Product> productRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.SoftDelete();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}