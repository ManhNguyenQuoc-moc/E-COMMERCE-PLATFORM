using FluentValidation; // Import thư viện FluentValidation

namespace EcommercePlatform.Application.Features.Products.Queries.GetProducts; // Cùng namespace với GetProductsQuery

public sealed class GetProductsValidator : AbstractValidator<GetProductsQuery> // Validator chạy tự động qua ValidationBehavior trước khi vào Handler
{
    public GetProductsValidator() // Khai báo các rule kiểm tra tham số query
    {
        RuleFor(x => x.Page) // Kiểm tra số trang
            .GreaterThanOrEqualTo(1) // Trang phải >= 1
            .WithMessage("Page must be greater than or equal to 1."); // Thông báo lỗi nếu < 1

        RuleFor(x => x.Size) // Kiểm tra kích thước trang
            .InclusiveBetween(1, 100) // Giới hạn từ 1 đến 100 để tránh query quá tải DB
            .WithMessage("Size must be between 1 and 100."); // Thông báo lỗi nếu ngoài khoảng 1..100

        RuleFor(x => x.MinPrice) // Kiểm tra giá tối thiểu
            .GreaterThanOrEqualTo(0) // Giá không được âm
            .When(x => x.MinPrice.HasValue) // Chỉ kiểm tra khi client có truyền MinPrice
            .WithMessage("MinPrice must be greater than or equal to 0."); // Thông báo lỗi nếu âm

        RuleFor(x => x.MaxPrice) // Kiểm tra giá tối đa
            .GreaterThanOrEqualTo(0) // Giá tối đa không được âm
            .When(x => x.MaxPrice.HasValue) // Chỉ kiểm tra khi có truyền MaxPrice
            .WithMessage("MaxPrice must be greater than or equal to 0."); // Thông báo lỗi nếu âm

        RuleFor(x => x.MaxPrice) // Kiểm tra quan hệ khoảng giá
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value) // MaxPrice phải >= MinPrice
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue) // Chỉ kiểm tra khi truyền cả 2 cận giá
            .WithMessage("MaxPrice must be greater than or equal to MinPrice."); // Thông báo lỗi nếu khoảng giá ngược
    }
}