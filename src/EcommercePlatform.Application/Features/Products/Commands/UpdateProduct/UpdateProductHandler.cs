using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MediatR;

namespace EcommercePlatform.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IRepository<Product> productRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.UpdateDetails(request.Name, request.Description, request.Price);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
