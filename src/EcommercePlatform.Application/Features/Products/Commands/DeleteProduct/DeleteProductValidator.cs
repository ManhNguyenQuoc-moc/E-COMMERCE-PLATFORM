using FluentValidation;

namespace EcommercePlatform.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");
    }
}