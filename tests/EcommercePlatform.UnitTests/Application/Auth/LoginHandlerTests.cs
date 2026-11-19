using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Auth.Commands.Login;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Auth;

public sealed class LoginHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<Account> _accountRepository;
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly IMapper _mapper;

    public LoginHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _accountRepository = new Repository<Account>(_dbContext);

        var config = new TypeAdapterConfig();
        new AuthMapProfile().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task Handle_Should_ReturnAuthResponseAndRecordLogin_When_CredentialsAreValid()
    {
        // Arrange
        var account = new Account("john_doe", "john@example.com", "hashed_pwd", UserRole.User);
        var user = new User(account.Id, "John Doe");
        _dbContext.Accounts.Add(account);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(p => p.VerifyPassword("Password@123", "hashed_pwd"))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateToken(user.Id, account.Id, "john_doe", "john@example.com", UserRole.User))
            .Returns("valid_jwt_token");

        var handler = new LoginHandler(
            _accountRepository,
            _dbContext,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(new LoginCommand("john@example.com", "Password@123"), CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal("john_doe", result.Username);
        Assert.Equal("valid_jwt_token", result.AccessToken);

        var updatedAccount = await _dbContext.Accounts.FirstAsync(a => a.Id == account.Id);
        Assert.NotNull(updatedAccount.LastLoginAt);
    }

    [Fact]
    public async Task Handle_Should_VerifyDummyHashAndThrowUnauthorized_When_AccountDoesNotExist()
    {
        // Arrange
        _passwordHasherMock
            .Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var handler = new LoginHandler(
            _accountRepository,
            _dbContext,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("nonexistent@example.com", "Password@123"), CancellationToken.None));

        // Verify anti-timing attack: VerifyPassword was still called once with the dummy hash
        _passwordHasherMock.Verify(
            p => p.VerifyPassword("Password@123", It.Is<string>(h => h.StartsWith("$2a$11$"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_PasswordIsInvalid()
    {
        // Arrange
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(p => p.VerifyPassword("WrongPassword", "hashed_pwd"))
            .Returns(false);

        var handler = new LoginHandler(
            _accountRepository,
            _dbContext,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("john_doe", "WrongPassword"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_AccountIsDeactivated()
    {
        // Arrange
        var account = new Account("john_doe", "john@example.com", "hashed_pwd");
        account.Deactivate();
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(p => p.VerifyPassword("Password@123", "hashed_pwd"))
            .Returns(true);

        var handler = new LoginHandler(
            _accountRepository,
            _dbContext,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("john_doe", "Password@123"), CancellationToken.None));

        Assert.Equal("Account is deactivated.", ex.Message);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
