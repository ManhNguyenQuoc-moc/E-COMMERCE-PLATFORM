using EcommercePlatform.Application.Features.Auth.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
