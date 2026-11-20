using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EcommercePlatform.Infrastructure.Services;

public sealed class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public GeneratedTokenResult GenerateAccessToken(
        Guid userId,
        Guid accountId,
        string username,
        string email,
        UserRole role)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "EcommercePlatform.Api";
        var audience = configuration["Jwt:Audience"] ?? "EcommercePlatform.Client";
        var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roleName = role.ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
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
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new GeneratedTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public GeneratedTokenResult GenerateRefreshToken()
    {
        var expiryDays = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var days) && days > 0
            ? days
            : 7;

        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return new GeneratedTokenResult(rawToken, DateTimeOffset.UtcNow.AddDays(expiryDays));
    }

    public string HashRefreshToken(string rawRefreshToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken));
        return Convert.ToHexStringLower(hashBytes);
    }
}
