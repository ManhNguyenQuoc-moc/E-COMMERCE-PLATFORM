using EcommercePlatform.Application.Features.Coupons.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;

namespace EcommercePlatform.Application.Common.Mappings;

public sealed class CouponMapProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Coupon, CouponDto>()
            .Map(dest => dest.DiscountType, src => src.DiscountType.ToString());
    }
}
