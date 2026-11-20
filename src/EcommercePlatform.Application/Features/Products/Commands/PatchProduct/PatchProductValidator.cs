using FluentValidation;

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct;

public sealed class PatchProductValidator : AbstractValidator<PatchProductCommand>
{
    public PatchProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Description is not null || x.Price.HasValue || x.CategoryId.HasValue)
            .WithMessage("At least one field must be provided for partial update.");

        RuleFor(x => x.Name!)
            .NotEmpty().WithMessage("Product name cannot be empty.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description!)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Price!.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.CategoryId)
            .Must(id => id != Guid.Empty).WithMessage("CategoryId must not be an empty GUID.")
            .When(x => x.CategoryId.HasValue);
    }
}