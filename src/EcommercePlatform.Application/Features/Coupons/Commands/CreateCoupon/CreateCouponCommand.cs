using EcommercePlatform.Application.Features.Coupons.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Coupons.Commands.CreateCoupon;

public sealed record CreateCouponCommand(
    string Code,
    string DiscountType,
    decimal Value,
    decimal MinOrderValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int UsageLimit) : IRequest<CouponDto>;
