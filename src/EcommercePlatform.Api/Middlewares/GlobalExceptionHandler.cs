using EcommercePlatform.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Api.Middlewares;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(httpContext, validationException),
            NotFoundException or KeyNotFoundException => CreateProblemDetails(
                httpContext,
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                exception.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            UnauthorizedAccessException => CreateProblemDetails(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                !string.IsNullOrWhiteSpace(exception.Message)
                    ? exception.Message
                    : "Authentication is required or credentials are invalid.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            DomainException or BadHttpRequestException => CreateProblemDetails(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            DbUpdateException => CreateProblemDetails(
                httpContext,
                StatusCodes.Status409Conflict,
                "Resource Conflict",
                "A concurrency or uniqueness conflict occurred while saving data.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => CreateProblemDetails(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred while processing your request.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    public static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string type)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problemDetails;
    }

    public static async Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string type,
        CancellationToken cancellationToken = default)
    {
        var problemDetails = CreateProblemDetails(httpContext, statusCode, title, detail, type);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }

    private static ProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName)
                ? string.Empty
                : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = CreateProblemDetails(
            httpContext,
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            validationException.Errors.FirstOrDefault()?.ErrorMessage
                ?? "One or more validation errors occurred.",
            "https://tools.ietf.org/html/rfc9110#section-15.5.1");

        problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }
}
