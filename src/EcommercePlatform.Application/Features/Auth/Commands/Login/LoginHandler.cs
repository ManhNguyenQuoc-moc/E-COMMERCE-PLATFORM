using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler(
    IRepository<Account> accountRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IMapper mapper) : IRequestHandler<LoginCommand, AuthResponseDto>
{
    // Dummy BCrypt hash used to equalize response latency when account is not found (prevents user enumeration timing attacks).
    private const string DummyPasswordHash = "$2a$11$c7FLo2elzEW8q3cP0bDaCusgS7gAHpTr8pVCqYfSY6OjjuKRpsN0m";

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.EmailOrUsername.Trim();
        var normalizedEmail = input.ToLowerInvariant();

        var account = await accountRepository
            .AsQueryable(asNoTracking: false)
            .Include(a => a.User)
            .FirstOrDefaultAsync(
                a => a.Email == normalizedEmail || a.Username == input,
                cancellationToken);

        var hashToVerify = account?.PasswordHash ?? DummyPasswordHash;
        var isPasswordValid = passwordHasher.VerifyPassword(request.Password, hashToVerify);

        if (account is null || !isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email/username or password.");
        }

        if (!account.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        account.RecordLogin();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userId = account.User?.Id ?? account.Id;

        var token = jwtTokenGenerator.GenerateToken(
            userId,
            account.Id,
            account.Username,
            account.Email,
            account.Role);

        return mapper.Map<AuthResponseDto>((account, account.User, token));
    }
}
