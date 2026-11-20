using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Orders.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(int Page = 1, int Size = 10) : IRequest<PaginatedList<OrderDto>>;
