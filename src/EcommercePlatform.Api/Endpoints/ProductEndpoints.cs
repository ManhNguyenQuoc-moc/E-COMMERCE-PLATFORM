using EcommercePlatform.Application.Common.Models; // Import PaginatedList<T>
using EcommercePlatform.Application.Features.Products.Commands.CreateProduct; // Import CreateProductCommand
using EcommercePlatform.Application.Features.Products.Commands.DeleteProduct; // Import DeleteProductCommand
using EcommercePlatform.Application.Features.Products.Commands.UpdateProduct; // Import UpdateProductCommand
using EcommercePlatform.Application.Features.Products.DTOs; // Import ProductDto
using EcommercePlatform.Application.Features.Products.Queries.GetProductById; // Import GetProductByIdQuery
using EcommercePlatform.Application.Features.Products.Queries.GetProducts;
using EcommercePlatform.Application.Features.Products.Commands.PatchProduct; // Import PatchProductCommand
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
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .WithName("GetProducts")
            .AllowAnonymous()
            .Produces<PaginatedList<ProductDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
                return TypedResults.Ok(product);
            })
            .WithName("GetProductById")
            .AllowAnonymous()
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateProductCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var productId = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/products/{productId}", new { Id = productId });
            })
            .WithName("CreateProduct")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.CategoryId),
                    cancellationToken);

                return TypedResults.NoContent();
            })
            .WithName("UpdateProduct")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPatch("/{id:guid}", async (
                Guid id,
                PatchProductRequest request,
                ISender sender, // Inject MediatR ISender
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new PatchProductCommand(id, request.Name, request.Description, request.Price, request.CategoryId),
                    cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("PatchProduct")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteProductCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteProduct")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    Guid? CategoryId = null);

public sealed record PatchProductRequest(
    string? Name = null,
    string? Description = null,
    decimal? Price = null,
    Guid? CategoryId = null);
