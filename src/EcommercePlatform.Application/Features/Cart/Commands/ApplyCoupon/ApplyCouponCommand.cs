using EcommercePlatform.Application.Features.Cart.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Cart.Commands.ApplyCoupon;

public sealed record ApplyCouponCommand(string Code) : IRequest<CartDto>;
