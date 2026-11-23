namespace EcommercePlatform.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    Guid? UserGuid { get; }
    Guid? AccountGuid { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
