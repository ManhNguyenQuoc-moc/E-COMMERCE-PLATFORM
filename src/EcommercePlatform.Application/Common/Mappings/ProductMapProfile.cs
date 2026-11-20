using EcommercePlatform.Application.Features.Products.Commands.CreateProduct;
using EcommercePlatform.Application.Features.Products.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;

namespace EcommercePlatform.Application.Common.Mappings;

public sealed class ProductMapProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductDto>();

        config.NewConfig<CreateProductCommand, Product>()
            .ConstructUsing(src => new Product(
                src.Name,
                src.Description,
                src.Sku,
                src.Price,
                src.StockQuantity));
    }
}
