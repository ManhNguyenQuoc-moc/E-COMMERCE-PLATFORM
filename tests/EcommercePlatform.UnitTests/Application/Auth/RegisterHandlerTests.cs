using System.Linq.Expressions;
using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Common.Mappings;
using EcommercePlatform.Application.Features.Auth.Commands.Register;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using Mapster;
using MapsterMapper;
using Moq;

namespace EcommercePlatform.UnitTests.Application.Auth;

public sealed class RegisterHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock = new();
    private readonly Mock<IRepository<User>> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly IMapper _mapper;

    public RegisterHandlerTests()
    {
        var config = new TypeAdapterConfig();
        new AuthMapProfile().Register(config);
        _mapper = new Mapper(config);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<EcommercePlatform.Application.Features.Auth.DTOs.AuthResponseDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EcommercePlatform.Application.Features.Auth.DTOs.AuthResponseDto>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    [Fact]
    public async Task Handle_Should_CreateAccountAndUserAndReturnToken_When_InputIsValid()
    {
        // Arrange
        var command = new RegisterCommand(
            "john_doe",
            "John@Example.com",
            "Password@123",
            "John Doe",
            "0901234567",
            "123 Street");

        _accountRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(p => p.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateToken(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                "john_doe",
                "john@example.com",
                UserRole.User))
            .Returns("mock_jwt_token");

        var handler = new RegisterHandler(
            _accountRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.NotEqual(Guid.Empty, result.AccountId);
        Assert.Equal("john_doe", result.Username);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("User", result.Role);
        Assert.Equal("mock_jwt_token", result.AccessToken);

        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_EmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("john_doe", "john@example.com", "Password@123", "John Doe");

        _accountRepoMock
            .SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterHandler(
            _accountRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_UsernameAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("john_doe", "john@example.com", "Password@123", "John Doe");

        _accountRepoMock
            .SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var handler = new RegisterHandler(
            _accountRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
