using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.UnitTests.Domain;

public sealed class AccountTests
{
    [Fact]
    public void Constructor_Should_CreateActiveAccountWithNormalizedEmail_When_InputsAreValid()
    {
        var account = new Account("  john_doe ", "  John.Doe@Example.com ", "hashed_pwd", UserRole.User);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("john_doe", account.Username);
        Assert.Equal("john.doe@example.com", account.Email);
        Assert.Equal("hashed_pwd", account.PasswordHash);
        Assert.Equal(UserRole.User, account.Role);
        Assert.True(account.IsActive);
        Assert.Null(account.LastLoginAt);
    }

    [Theory]
    [InlineData("", "test@example.com", "hash")]
    [InlineData("   ", "test@example.com", "hash")]
    [InlineData("user", "", "hash")]
    [InlineData("user", "invalid-email", "hash")]
    [InlineData("user", "test@example.com", "")]
    public void Constructor_Should_ThrowDomainException_When_InputIsInvalid(
        string username,
        string email,
        string passwordHash)
    {
        Assert.Throws<DomainException>(() => new Account(username, email, passwordHash));
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_AdminUsernameStartsWithSystemPrefix()
    {
        Assert.Throws<DomainException>(() =>
            new Account("System.Root", "admin@example.com", "hashed_pwd", UserRole.Admin));
    }

    [Fact]
    public void RecordLogin_Should_UpdateLastLoginAt_When_AccountIsActive()
    {
        var account = new Account("john", "john@example.com", "hash");

        account.RecordLogin();

        Assert.NotNull(account.LastLoginAt);
        Assert.NotNull(account.UpdatedAt);
    }

    [Fact]
    public void RecordLogin_Should_ThrowDomainException_When_AccountIsDeactivated()
    {
        var account = new Account("john", "john@example.com", "hash");
        account.Deactivate();

        Assert.False(account.IsActive);
        Assert.Throws<DomainException>(() => account.RecordLogin());
    }
}
