using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;

namespace EcommercePlatform.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler(
    IRepository<Account> accountRepository,
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var trimmedUsername = request.Username.Trim();

        if (await accountRepository.AnyAsync(a => a.Email == normalizedEmail, cancellationToken))
        {
            throw new DomainException($"Email '{normalizedEmail}' is already registered.");
        }

        if (await accountRepository.AnyAsync(a => a.Username == trimmedUsername, cancellationToken))
        {
            throw new DomainException($"Username '{trimmedUsername}' is already taken.");
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var account = new Account(trimmedUsername, normalizedEmail, passwordHash, UserRole.User);
            var user = new User(
                account.Id,
                request.FullName,
                request.PhoneNumber,
                request.DefaultShippingAddress);

            await accountRepository.AddAsync(account, ct);
            await userRepository.AddAsync(user, ct);

            var token = jwtTokenGenerator.GenerateToken(
                user.Id,
                account.Id,
                account.Username,
                account.Email,
                account.Role);

            return mapper.Map<AuthResponseDto>((account, (User?)user, token));
        }, cancellationToken);
    }
}
