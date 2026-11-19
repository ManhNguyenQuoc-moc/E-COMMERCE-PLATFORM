using EcommercePlatform.Application.Features.Auth.DTOs;
using EcommercePlatform.Domain.Entities;
using Mapster;

namespace EcommercePlatform.Application.Common.Mappings;

public sealed class AuthMapProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<(Account Account, User? User, string AccessToken), AuthResponseDto>()
            .Map(dest => dest.UserId, src => src.User != null ? src.User.Id : src.Account.Id)
            .Map(dest => dest.AccountId, src => src.Account.Id)
            .Map(dest => dest.Username, src => src.Account.Username)
            .Map(dest => dest.Email, src => src.Account.Email)
            .Map(dest => dest.FullName, src => src.User != null ? src.User.FullName : src.Account.Username)
            .Map(dest => dest.Role, src => src.Account.Role.ToString())
            .Map(dest => dest.AccessToken, src => src.AccessToken);
    }
}
