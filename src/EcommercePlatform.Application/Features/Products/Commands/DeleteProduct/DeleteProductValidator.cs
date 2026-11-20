using FluentValidation; // Import thư viện FluentValidation

namespace EcommercePlatform.Application.Features.Products.Commands.DeleteProduct; // Cùng namespace với DeleteProductCommand

public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand> // Validator kiểm tra Id xóa sản phẩm
{
    public DeleteProductValidator() // Khai báo rule validation
    {
        RuleFor(x => x.Id) // Kiểm tra Id sản phẩm cần xóa
            .NotEmpty().WithMessage("Product ID is required."); // Chặn Guid.Empty ngay ở pipeline trước khi query DB
    }
}