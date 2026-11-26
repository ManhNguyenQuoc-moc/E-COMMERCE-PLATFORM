using EcommercePlatform.Application.Features.Products.Queries.GetProductById;
using EcommercePlatform.Domain.Exceptions;
using Grpc.Core;
using MediatR;

namespace EcommercePlatform.Api.Grpc;

public sealed class CatalogGrpcService(ISender sender) : CatalogGrpc.CatalogGrpcBase
{
    public override async Task<ProductReply> GetProduct(
        GetProductRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var productId) || productId == Guid.Empty)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ID must be a valid non-empty GUID."));
        }

        try
        {
            var product = await sender.Send(new GetProductByIdQuery(productId), context.CancellationToken);

            return new ProductReply
            {
                Id = product.Id.ToString(),
                Name = product.Name,
                Sku = product.Sku,
                Price = (double)product.Price,
                StockQuantity = product.StockQuantity,
                Status = product.Status.ToString(),
                CategoryName = product.CategoryName ?? string.Empty
            };
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }
}
