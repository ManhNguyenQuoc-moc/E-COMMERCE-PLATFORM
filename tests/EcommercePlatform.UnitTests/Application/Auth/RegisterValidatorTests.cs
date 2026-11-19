using EcommercePlatform.Application.Features.Auth.Commands.Register;

namespace EcommercePlatform.UnitTests.Application.Auth;

public sealed class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_CommandIsValid()
    {
        var command = new RegisterCommand(
            "john_doe",
            "john@example.com",
            "Password@123",
            "John Doe",
            "0901234567",
            "123 Street");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_Fail_When_PasswordExceeds72BytesLimit()
    {
        var tooLongPassword = new string('a', 73);
        var command = new RegisterCommand(
            "john_doe",
            "john@example.com",
            tooLongPassword,
            "John Doe");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Theory]
    [InlineData("ab", "john@example.com", "Password@123", "John Doe")]
    [InlineData("john_doe", "not-an-email", "Password@123", "John Doe")]
    [InlineData("john_doe", "john@example.com", "12345", "John Doe")]
    [InlineData("john_doe", "john@example.com", "Password@123", "")]
    public void Validate_Should_Fail_When_RequiredFieldsAreInvalid(
        string username,
        string email,
        string password,
        string fullName)
    {
        var command = new RegisterCommand(username, email, password, fullName);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
