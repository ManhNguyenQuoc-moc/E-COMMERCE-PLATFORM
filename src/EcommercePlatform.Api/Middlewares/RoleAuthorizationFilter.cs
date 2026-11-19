using System.Security.Claims;
using EcommercePlatform.Domain.Enums;

namespace EcommercePlatform.Api.Middlewares;

public sealed class RoleAuthorizationFilter(params UserRole[] allowedRoles) : IEndpointFilter
{
    private readonly HashSet<string> _allowedRoleNames = allowedRoles
        .Select(r => r.ToString())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return TypedResults.Problem(GlobalExceptionHandler.CreateProblemDetails(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"));
        }

        var userRole = httpContext.User.FindFirstValue(ClaimTypes.Role)
                       ?? httpContext.User.FindFirstValue("roles");

        if (string.IsNullOrWhiteSpace(userRole) || !_allowedRoleNames.Contains(userRole))
        {
            var requiredRoles = string.Join(", ", _allowedRoleNames);
            return TypedResults.Problem(GlobalExceptionHandler.CreateProblemDetails(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                $"You do not have permission to perform this action. Required role(s): {requiredRoles}.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.4"));
        }

        return await next(context);
    }
}

public static class RoleAuthorizationFilterExtensions
{
    public static TBuilder RequireRole<TBuilder>(
        this TBuilder builder,
        params UserRole[] allowedRoles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter(new RoleAuthorizationFilter(allowedRoles));
    }
}
