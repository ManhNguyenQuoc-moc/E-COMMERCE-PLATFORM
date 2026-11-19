using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EcommercePlatform.Infrastructure.Services;

public sealed class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public string GenerateToken(Guid userId, Guid accountId, string username, string email, UserRole role)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "EcommercePlatform.Api";
        var audience = configuration["Jwt:Audience"] ?? "EcommercePlatform.Client";
        var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roleName = role.ToString();

        var claims = new[]
        {
            // muốn lấy gì thì lấy ở đây 
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("userId", userId.ToString()),
            new Claim("accountId", accountId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("username", username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("roles", roleName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
