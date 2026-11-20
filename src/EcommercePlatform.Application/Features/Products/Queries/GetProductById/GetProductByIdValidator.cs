using FluentValidation; // Import thư viện FluentValidation

namespace EcommercePlatform.Application.Features.Products.Queries.GetProductById; // Cùng namespace với GetProductByIdQuery

public sealed class GetProductByIdValidator : AbstractValidator<GetProductByIdQuery> // Validator kiểm tra Id khi xem chi tiết sản phẩm
{
    public GetProductByIdValidator() // Khai báo rule validation
    {
        RuleFor(x => x.Id) // Kiểm tra Id sản phẩm
            .NotEmpty().WithMessage("Product ID is required."); // Chặn Guid.Empty ngay ở pipeline trước khi query DB
    }
}
