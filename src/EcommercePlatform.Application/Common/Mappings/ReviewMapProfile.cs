using EcommercePlatform.Application.Features.Reviews.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;

namespace EcommercePlatform.Application.Common.Mappings;

public sealed class ReviewMapProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Review, ReviewDto>()
            .Map(dest => dest.UserFullName, src => src.User != null ? src.User.FullName : string.Empty);

        config.NewConfig<(Review Review, string UserFullName), ReviewDto>()
            .Map(dest => dest.Id, src => src.Review.Id)
            .Map(dest => dest.ProductId, src => src.Review.ProductId)
            .Map(dest => dest.UserId, src => src.Review.UserId)
            .Map(dest => dest.UserFullName, src => src.UserFullName)
            .Map(dest => dest.OrderId, src => src.Review.OrderId)
            .Map(dest => dest.Rating, src => src.Review.Rating)
            .Map(dest => dest.Comment, src => src.Review.Comment)
            .Map(dest => dest.CreatedAt, src => src.Review.CreatedAt);
    }
}
