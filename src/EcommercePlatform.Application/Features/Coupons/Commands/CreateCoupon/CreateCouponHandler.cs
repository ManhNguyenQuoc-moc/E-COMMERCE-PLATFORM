using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Coupons.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;

public sealed class CreateCouponHandler(
    IRepository<Coupon> couponRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateCouponCommand, CouponDto>
{
    public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        if (await couponRepository.AnyAsync(c => c.Code == normalizedCode, cancellationToken))
        {
            throw new DomainException($"Coupon code '{normalizedCode}' already exists.");
        }

        if (!CreateCouponValidator.TryParseDiscountType(request.DiscountType, out var discountType))
        {
            throw new DomainException("Invalid discount type.");
        }

        var coupon = new Coupon(
            normalizedCode,
            discountType,
            request.Value,
            request.MinOrderValue,
            request.StartDate,
            request.EndDate,
            request.UsageLimit);

        await couponRepository.AddAsync(coupon, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<CouponDto>(coupon);
    }
}
