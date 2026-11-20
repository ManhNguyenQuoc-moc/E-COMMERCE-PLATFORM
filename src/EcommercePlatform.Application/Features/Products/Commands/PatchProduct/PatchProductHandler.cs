using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions; // Import NotFoundException
using MediatR; // Import IRequestHandler

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct;

public sealed class PatchProductHandler(
    IRepository<Product> productRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<PatchProductCommand>
{
    public async Task Handle(PatchProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await categoryRepository.AnyAsync(
                c => c.Id == request.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new NotFoundException(nameof(Category), request.CategoryId.Value);
            }
        }

        product.UpdateDetails(
            request.Name ?? product.Name,
            request.Description ?? product.Description,
            request.Price ?? product.Price,
            request.CategoryId ?? product.CategoryId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}