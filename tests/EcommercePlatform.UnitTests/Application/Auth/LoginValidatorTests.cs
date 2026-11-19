using EcommercePlatform.Application.Features.Auth.Commands.Login;

namespace EcommercePlatform.UnitTests.Application.Auth;

public sealed class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_CommandIsValid()
    {
        var command = new LoginCommand("john@example.com", "Password@123");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Password@123")]
    [InlineData("john@example.com", "")]
    public void Validate_Should_Fail_When_FieldsAreEmpty(string emailOrUsername, string password)
    {
        var command = new LoginCommand(emailOrUsername, password);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
