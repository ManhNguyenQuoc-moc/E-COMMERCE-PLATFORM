using EcommercePlatform.Infrastructure.Services;

namespace EcommercePlatform.UnitTests.Infrastructure;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public void HashPassword_And_VerifyPassword_Should_ReturnTrue_ForMatchingPassword()
    {
        const string password = "Password@123";

        var hash = _passwordHasher.HashPassword(password);

        Assert.NotEqual(password, hash);
        Assert.True(_passwordHasher.VerifyPassword(password, hash));
        Assert.False(_passwordHasher.VerifyPassword("WrongPassword", hash));
    }
}
