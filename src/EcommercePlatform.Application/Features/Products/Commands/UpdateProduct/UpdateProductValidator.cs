using FluentValidation; // Import thư viện FluentValidation

namespace EcommercePlatform.Application.Features.Products.Commands.UpdateProduct; // Cùng namespace với UpdateProductCommand

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand> // Validator kiểm tra input thuần (Step 0 - Bước 2)
{
    public UpdateProductValidator() // Khai báo các rule validation
    {
        RuleFor(x => x.Id) // Kiểm tra Id sản phẩm
            .NotEmpty().WithMessage("Product ID is required."); // Không được là Guid.Empty

        RuleFor(x => x.Name) // Kiểm tra tên sản phẩm
            .NotEmpty().WithMessage("Product name is required.") // Không được rỗng hoặc chỉ toàn khoảng trắng
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters."); // Khớp HasMaxLength(200) trong ProductConfiguration

        RuleFor(x => x.Description) // Kiểm tra mô tả
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters."); // Khớp HasMaxLength(2000) trong ProductConfiguration

        RuleFor(x => x.Price) // Kiểm tra giá bán
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0."); // Giá không được âm

        RuleFor(x => x.CategoryId) // Kiểm tra CategoryId nếu client có truyền
            .Must(id => id != Guid.Empty) // Không cho phép truyền Guid.Empty
            .When(x => x.CategoryId.HasValue) // Chỉ kiểm tra khi CategoryId khác null
            .WithMessage("CategoryId must not be an empty GUID."); // Thông báo lỗi khi truyền Guid rỗng
    }
}