using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;
