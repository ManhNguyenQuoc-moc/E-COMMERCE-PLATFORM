using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    IRepository<Domain.Entities.RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.HashRefreshToken(request.RefreshToken.Trim());

        var existingToken = await refreshTokenRepository
            .AsQueryable(asNoTracking: false)
            .Include(rt => rt.Account!)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is not null && existingToken.IsRevoked && !string.IsNullOrEmpty(existingToken.ReplacedByTokenHash))
        {
            var activeTokens = await refreshTokenRepository
                .AsQueryable(asNoTracking: false)
                .Where(rt => rt.AccountId == existingToken.AccountId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token reuse detected. All active sessions have been revoked.");
        }

        if (existingToken is null || !existingToken.IsActive || existingToken.Account is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        if (!existingToken.Account.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        var account = existingToken.Account;
        var userId = account.User?.Id ?? account.Id;

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var newAccessToken = jwtTokenGenerator.GenerateAccessToken(
                userId,
                account.Id,
                account.Username,
                account.Email,
                account.Role);

            var newRefreshTokenResult = jwtTokenGenerator.GenerateRefreshToken();
            var effectiveExpiresAt = newRefreshTokenResult.ExpiresAt <= existingToken.AbsoluteExpiresAt
                ? newRefreshTokenResult.ExpiresAt
                : existingToken.AbsoluteExpiresAt;
            var effectiveRefreshTokenResult = new GeneratedTokenResult(newRefreshTokenResult.Token, effectiveExpiresAt);

            var newRefreshTokenHash = jwtTokenGenerator.HashRefreshToken(newRefreshTokenResult.Token);
            var newRefreshToken = new Domain.Entities.RefreshToken(
                account.Id,
                newRefreshTokenHash,
                effectiveExpiresAt,
                existingToken.AbsoluteExpiresAt);

            existingToken.Revoke(newRefreshTokenHash);
            refreshTokenRepository.Update(existingToken);
            await refreshTokenRepository.AddAsync(newRefreshToken, ct);

            return mapper.Map<AuthResponseDto>((account, account.User, newAccessToken, effectiveRefreshTokenResult));
        }, cancellationToken);
    }
}
