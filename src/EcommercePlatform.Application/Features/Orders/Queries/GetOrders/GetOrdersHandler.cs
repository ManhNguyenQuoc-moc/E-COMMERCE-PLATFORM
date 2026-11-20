using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Cart.Common;
using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Orders.Queries.GetOrders;

public sealed class GetOrdersHandler(
    IRepository<Order> orderRepository,
    ICurrentUser currentUser,
    IMapper mapper) : IRequestHandler<GetOrdersQuery, PaginatedList<OrderDto>>
{
    public async Task<PaginatedList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = CartQueryHelper.RequireAuthenticatedUserId(currentUser);

        var query = orderRepository
            .AsQueryable(asNoTracking: true)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ProjectToType<OrderDto>(mapper.Config);

        return await PaginatedList<OrderDto>.CreateAsync(query, request.Page, request.Size, cancellationToken);
    }
}
