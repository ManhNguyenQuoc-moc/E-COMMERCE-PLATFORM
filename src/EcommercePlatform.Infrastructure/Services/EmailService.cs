using EcommercePlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommercePlatform.Infrastructure.Services;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailSender
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending email to {Recipient} with subject {Subject}", to, subject);
        return Task.CompletedTask;
    }
}

