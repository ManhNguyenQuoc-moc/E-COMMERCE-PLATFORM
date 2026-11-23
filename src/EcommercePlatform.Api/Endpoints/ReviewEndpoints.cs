using EcommercePlatform.Application.Features.Reviews.Commands.CreateReview;
using EcommercePlatform.Application.Features.Reviews.DTOs;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reviews")
            .WithTags("Reviews")
            .RequireAuthorization("UserOrAdmin");

        group.MapPost("/", async (
                CreateReviewCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var review = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/reviews/{review.Id}", review);
            })
            .WithName("CreateReview")
            .RequireRateLimiting("CartWritePolicy")
            .Produces<ReviewDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }
}
