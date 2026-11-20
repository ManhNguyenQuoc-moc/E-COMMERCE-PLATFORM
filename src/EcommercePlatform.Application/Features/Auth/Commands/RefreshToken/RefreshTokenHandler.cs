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
            var newRefreshTokenHash = jwtTokenGenerator.HashRefreshToken(newRefreshTokenResult.Token);
            var newRefreshToken = new Domain.Entities.RefreshToken(
                account.Id,
                newRefreshTokenHash,
                newRefreshTokenResult.ExpiresAt);

            existingToken.Revoke(newRefreshTokenHash);
            refreshTokenRepository.Update(existingToken);
            await refreshTokenRepository.AddAsync(newRefreshToken, ct);

            return mapper.Map<AuthResponseDto>((account, account.User, newAccessToken, newRefreshTokenResult));
        }, cancellationToken);
    }
}
