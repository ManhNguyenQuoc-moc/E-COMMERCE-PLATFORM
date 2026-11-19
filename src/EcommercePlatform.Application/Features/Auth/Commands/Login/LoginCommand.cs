using EcommercePlatform.Application.Features.Auth.DTOs;
using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string EmailOrUsername,
    string Password) : IRequest<AuthResponseDto>;
