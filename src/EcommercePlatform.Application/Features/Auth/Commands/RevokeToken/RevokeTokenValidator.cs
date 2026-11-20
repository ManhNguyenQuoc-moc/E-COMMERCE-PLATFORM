using FluentValidation;

namespace EcommercePlatform.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
