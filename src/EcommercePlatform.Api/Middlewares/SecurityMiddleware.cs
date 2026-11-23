using System.Security.Claims;
using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace EcommercePlatform.Api.Middlewares;

public sealed class SecurityMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan AccountStatusCacheDuration = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(
        HttpContext context,
        IRepository<Account> accountRepository,
        IMemoryCache memoryCache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accountIdClaim = context.User.FindFirstValue("accountId");

            if (Guid.TryParse(accountIdClaim, out var accountId))
            {
                var cacheKey = $"account_active:{accountId}";
                if (!memoryCache.TryGetValue(cacheKey, out bool isActive))
                {
                    isActive = await accountRepository.AnyAsync(
                        a => a.Id == accountId && a.IsActive,
                        context.RequestAborted);

                    memoryCache.Set(cacheKey, isActive, AccountStatusCacheDuration);
                }

                if (!isActive)
                {
                    await GlobalExceptionHandler.WriteProblemDetailsAsync(
                        context,
                        StatusCodes.Status403Forbidden,
                        "Account Deactivated",
                        "Your account has been deactivated or no longer exists.",
                        "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                        context.RequestAborted);
                    return;
                }
            }
        }

        await next(context);
    }
}
