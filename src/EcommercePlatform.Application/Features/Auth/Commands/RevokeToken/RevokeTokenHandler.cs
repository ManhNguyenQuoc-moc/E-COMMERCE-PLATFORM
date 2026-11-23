using EcommercePlatform.Application.Common.Interfaces;
using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenHandler(
    IRepository<Domain.Entities.RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtTokenGenerator,
    ICurrentUser currentUser) : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.HashRefreshToken(request.RefreshToken.Trim());

        var existingToken = await refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.TokenHash == tokenHash,
            asNoTracking: false,
            cancellationToken);

        if (existingToken is null || existingToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Invalid or already revoked refresh token.");
        }

        if (currentUser.AccountGuid.HasValue && existingToken.AccountId != currentUser.AccountGuid.Value)
        {
            throw new UnauthorizedAccessException("You cannot revoke another user's refresh token.");
        }

        existingToken.Revoke();
        refreshTokenRepository.Update(existingToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
