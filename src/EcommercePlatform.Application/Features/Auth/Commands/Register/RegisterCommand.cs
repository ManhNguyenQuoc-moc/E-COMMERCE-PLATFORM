using EcommercePlatform.Application.Features.Auth.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null,
    string? DefaultShippingAddress = null) : IRequest<AuthResponseDto>;
