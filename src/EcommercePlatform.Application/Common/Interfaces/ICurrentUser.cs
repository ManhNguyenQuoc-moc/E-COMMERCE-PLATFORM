namespace EcommercePlatform.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

