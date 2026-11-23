using EcommercePlatform.Application.Features.Orders.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, string Status) : IRequest<OrderDto>;
