using FluentValidation; // Import thư viện FluentValidation

namespace EcommercePlatform.Application.Features.Products.Commands.PatchProduct; // Cùng namespace với PatchProductCommand

public sealed class PatchProductValidator : AbstractValidator<PatchProductCommand> // Validator kiểm tra input cho PATCH
{
    public PatchProductValidator() // Khai báo các rule validation
    {
        RuleFor(x => x.Id) // Kiểm tra Id sản phẩm
            .NotEmpty().WithMessage("Product ID is required."); // Không được là Guid.Empty

        RuleFor(x => x) // Kiểm tra tổng thể command
            .Must(x => x.Name is not null || x.Description is not null || x.Price.HasValue || x.CategoryId.HasValue) // Bắt buộc phải gửi ít nhất 1 field để tránh request rỗng vô nghĩa
            .WithMessage("At least one field must be provided for partial update."); // Thông báo lỗi nếu tất cả các field đều null

        RuleFor(x => x.Name!) // Kiểm tra tên sản phẩm nếu có truyền lên
            .NotEmpty().WithMessage("Product name cannot be empty.") // Nếu đã truyền Name thì không được rỗng hoặc toàn khoảng trắng
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.") // Khớp HasMaxLength(200) trong ProductConfiguration
            .When(x => x.Name is not null); // Chỉ validate rule này khi Name khác null

        RuleFor(x => x.Description!) // Kiểm tra mô tả nếu có truyền lên
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.") // Khớp HasMaxLength(2000) trong ProductConfiguration
            .When(x => x.Description is not null); // Chỉ validate rule này khi Description khác null

        RuleFor(x => x.Price!.Value) // Kiểm tra giá bán nếu có truyền lên
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.") // Giá không được âm
            .When(x => x.Price.HasValue); // Chỉ validate rule này khi Price khác null

        RuleFor(x => x.CategoryId) // Kiểm tra CategoryId nếu có truyền lên
            .Must(id => id != Guid.Empty).WithMessage("CategoryId must not be an empty GUID.") // Không cho phép truyền Guid.Empty
            .When(x => x.CategoryId.HasValue); // Chỉ validate rule này khi CategoryId khác null
    }
}