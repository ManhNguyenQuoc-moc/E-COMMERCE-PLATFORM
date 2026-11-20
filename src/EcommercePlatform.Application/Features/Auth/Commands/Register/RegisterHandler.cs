using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;

using RefreshTokenEntity = EcommercePlatform.Domain.Entities.RefreshToken;

namespace EcommercePlatform.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler(
    IRepository<Account> accountRepository,
    IRepository<User> userRepository,
    IRepository<RefreshTokenEntity> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var trimmedUsername = request.Username.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (await accountRepository.AnyAsync(a => a.Email == normalizedEmail, cancellationToken))
        {
            throw new DomainException($"Email '{normalizedEmail}' is already registered.");
        }

        if (await accountRepository.AnyAsync(a => a.Username == trimmedUsername, cancellationToken))
        {
            throw new DomainException($"Username '{trimmedUsername}' is already taken.");
        }

        if (normalizedPhone is not null && await userRepository.AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken))
        {
            throw new DomainException($"Phone number '{normalizedPhone}' is already registered.");
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var account = new Account(trimmedUsername, normalizedEmail, passwordHash, UserRole.User);
            var user = new User(
                account.Id,
                request.FullName,
                normalizedPhone,
                request.DefaultShippingAddress);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(
                user.Id,
                account.Id,
                account.Username,
                account.Email,
                account.Role);

            var refreshTokenResult = jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenHash = jwtTokenGenerator.HashRefreshToken(refreshTokenResult.Token);
            var refreshToken = new RefreshTokenEntity(account.Id, refreshTokenHash, refreshTokenResult.ExpiresAt);

            await accountRepository.AddAsync(account, ct);
            await userRepository.AddAsync(user, ct);
            await refreshTokenRepository.AddAsync(refreshToken, ct);

            return mapper.Map<AuthResponseDto>((account, (User?)user, accessToken, refreshTokenResult));
        }, cancellationToken);
    }
}
