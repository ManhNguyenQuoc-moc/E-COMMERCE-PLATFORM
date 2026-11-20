using System.Net.Mail;
using EcommercePlatform.Domain.Common;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;

namespace EcommercePlatform.Domain.Entities;

public class Account : BaseEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastLoginAt { get; private set; }

    public User? User { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private Account()
    {
    }

    public Account(string username, string email, string passwordHash, UserRole role = UserRole.User)
    {
        var trimmedUsername = username?.Trim() ?? string.Empty;
        var trimmedEmail = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedUsername))
            throw new DomainException("Username is required.");

        if (string.IsNullOrWhiteSpace(trimmedEmail))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        if (!MailAddress.TryCreate(trimmedEmail, out _))
            throw new DomainException("Invalid email format.");

        if (role == UserRole.Admin && trimmedUsername.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Admin usernames cannot start with 'System.'.");

        Username = trimmedUsername;
        Email = trimmedEmail.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public void RecordLogin()
    {
        if (!IsActive)
            throw new DomainException("Account is deactivated.");

        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash is required.");

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
