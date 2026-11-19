using EcommercePlatform.Domain.Enums;

namespace EcommercePlatform.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, Guid accountId, string username, string email, UserRole role);
}