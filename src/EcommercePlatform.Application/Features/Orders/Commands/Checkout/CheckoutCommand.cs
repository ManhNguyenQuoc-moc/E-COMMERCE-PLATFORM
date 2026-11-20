using EcommercePlatform.Application.Features.Orders.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Orders.Commands.Checkout;

public sealed record CheckoutCommand(
    string ShippingRecipientName,
    string ShippingPhone,
    string ShippingAddress,
    string? Notes = null) : IRequest<OrderDto>;
