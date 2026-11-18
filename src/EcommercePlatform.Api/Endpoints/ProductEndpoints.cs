using EcommercePlatform.Application.Features.Products.Commands.CreateProduct;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Application.Features.Products.Queries.GetProductById;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
                return TypedResults.Ok(product);
            })
            .WithName("GetProductById")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateProductCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var productId = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/products/{productId}", new { Id = productId });
            })
            .WithName("CreateProduct")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return app;
    }
}

