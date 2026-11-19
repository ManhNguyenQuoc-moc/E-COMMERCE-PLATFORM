using EcommercePlatform.Application.Common.Models;
using EcommercePlatform.Application.Features.Products.Commands.CreateProduct;
using EcommercePlatform.Application.Features.Products.Commands.DeleteProduct;
using EcommercePlatform.Application.Features.Products.Commands.UpdateProduct;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Application.Features.Products.Queries.GetProductById;
using EcommercePlatform.Application.Features.Products.Queries.GetProducts;
using MediatR;

namespace EcommercePlatform.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products");

        group.MapGet("/", async (
                [AsParameters] GetProductsQuery query,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .WithName("GetProducts")
            .Produces<PaginatedList<ProductDto>>(StatusCodes.Status200OK);

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
                return TypedResults.Created($"/api/v1/products/{productId}", new { Id = productId });
            })
            .WithName("CreateProduct")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new UpdateProductCommand(id, request.Name, request.Description, request.Price),
                    cancellationToken);

                return TypedResults.NoContent();
            })
            .WithName("UpdateProduct")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteProductCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteProduct")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price);
