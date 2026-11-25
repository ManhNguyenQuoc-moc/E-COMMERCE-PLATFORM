using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Auth.Commands.RefreshToken;
using EcommercePlatform.Application.Features.Auth.Commands.RevokeToken;
using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Auth;

public sealed class RefreshAndRevokeTokenHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly IMapper _mapper;

    public RefreshAndRevokeTokenHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _refreshTokenRepository = new Repository<RefreshToken>(_dbContext);

        var config = new TypeAdapterConfig();
        new AuthMapProfile().Register(config);
        _mapper = new Mapper(config);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _dbContext.SaveChangesAsync(ct));

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<AuthResponseDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<AuthResponseDto>>, CancellationToken>(
                async (op, ct) =>
                {
                    var result = await op(ct);
                    await _dbContext.SaveChangesAsync(ct);
                    return result;
                });
    }

    [Fact]
    public async Task RefreshTokenHandler_Should_RotateTokens_When_RefreshTokenIsActive()
    {
        var account = new Account("john_doe", "john@example.com", "hashed_pwd", UserRole.User);
        var user = new User(account.Id, "John Doe");
        var oldToken = new RefreshToken(account.Id, "old_token_hash", DateTimeOffset.UtcNow.AddDays(7));

        _dbContext.Accounts.Add(account);
        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(oldToken);
        await _dbContext.SaveChangesAsync();

        var newAccessExpiry = DateTimeOffset.UtcNow.AddMinutes(15);
        var newRefreshExpiry = DateTimeOffset.UtcNow.AddDays(7);

        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_old_token")).Returns("old_token_hash");
        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateAccessToken(user.Id, account.Id, "john_doe", "john@example.com", UserRole.User))
            .Returns(new GeneratedTokenResult("new_access_token", newAccessExpiry));
        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns(new GeneratedTokenResult("raw_new_token", newRefreshExpiry));
        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_new_token")).Returns("new_token_hash");

        var handler = new RefreshTokenHandler(
            _refreshTokenRepository,
            _unitOfWorkMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        var result = await handler.Handle(new RefreshTokenCommand("raw_old_token"), CancellationToken.None);

        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("raw_new_token", result.RefreshToken);

        var dbOldToken = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Id == oldToken.Id);
        Assert.True(dbOldToken.IsRevoked);
        Assert.Equal("new_token_hash", dbOldToken.ReplacedByTokenHash);

        var dbNewToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == "new_token_hash");
        Assert.NotNull(dbNewToken);
        Assert.True(dbNewToken.IsActive);
    }

    [Fact]
    public async Task RefreshTokenHandler_Should_ThrowUnauthorized_When_TokenIsAlreadyRevoked()
    {
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        var revokedToken = new RefreshToken(account.Id, "revoked_hash", DateTimeOffset.UtcNow.AddDays(7));
        revokedToken.Revoke();

        _dbContext.Accounts.Add(account);
        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_revoked")).Returns("revoked_hash");

        var handler = new RefreshTokenHandler(
            _refreshTokenRepository,
            _unitOfWorkMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand("raw_revoked"), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshTokenHandler_Should_RevokeAllActiveTokens_When_RotatedTokenIsReused()
    {
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        var reusedOldToken = new RefreshToken(account.Id, "old_reused_hash", DateTimeOffset.UtcNow.AddDays(7));
        reusedOldToken.Revoke("current_active_hash");
        var currentActiveToken = new RefreshToken(account.Id, "current_active_hash", DateTimeOffset.UtcNow.AddDays(7));

        _dbContext.Accounts.Add(account);
        _dbContext.RefreshTokens.AddRange(reusedOldToken, currentActiveToken);
        await _dbContext.SaveChangesAsync();

        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_old_reused")).Returns("old_reused_hash");

        var handler = new RefreshTokenHandler(
            _refreshTokenRepository,
            _unitOfWorkMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand("raw_old_reused"), CancellationToken.None));

        var dbCurrentActive = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Id == currentActiveToken.Id);
        Assert.True(dbCurrentActive.IsRevoked);
    }

    [Fact]
    public async Task RevokeTokenHandler_Should_RevokeActiveToken_When_LoggingOut()
    {
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        var activeToken = new RefreshToken(account.Id, "active_hash", DateTimeOffset.UtcNow.AddDays(7));

        _dbContext.Accounts.Add(account);
        _dbContext.RefreshTokens.Add(activeToken);
        await _dbContext.SaveChangesAsync();

        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_active")).Returns("active_hash");
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.AccountGuid).Returns(account.Id);

        var handler = new RevokeTokenHandler(_refreshTokenRepository, _dbContext, _jwtTokenGeneratorMock.Object, currentUserMock.Object);

        await handler.Handle(new RevokeTokenCommand("raw_active"), CancellationToken.None);

        var updated = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Id == activeToken.Id);
        Assert.True(updated.IsRevoked);
    }

    [Fact]
    public async Task RevokeTokenHandler_Should_ThrowUnauthorized_When_RevokingAnotherUsersToken()
    {
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        var activeToken = new RefreshToken(account.Id, "active_hash", DateTimeOffset.UtcNow.AddDays(7));

        _dbContext.Accounts.Add(account);
        _dbContext.RefreshTokens.Add(activeToken);
        await _dbContext.SaveChangesAsync();

        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_active")).Returns("active_hash");
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.AccountGuid).Returns(Guid.NewGuid());

        var handler = new RevokeTokenHandler(_refreshTokenRepository, _dbContext, _jwtTokenGeneratorMock.Object, currentUserMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RevokeTokenCommand("raw_active"), CancellationToken.None));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
