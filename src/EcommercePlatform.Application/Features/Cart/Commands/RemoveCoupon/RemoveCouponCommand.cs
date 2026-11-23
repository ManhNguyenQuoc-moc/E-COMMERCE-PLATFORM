using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Cart.Commands.RemoveCoupon;

public sealed record RemoveCouponCommand : IRequest<CartDto>;
