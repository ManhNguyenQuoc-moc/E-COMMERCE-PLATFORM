using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;

namespace EcommercePlatform.Application.Common.Mappings;

public sealed class OrderMapProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderItem, OrderItemDto>()
            .Map(dest => dest.LineTotal, src => src.UnitPrice * src.Quantity);

        config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
