using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Models;
using AutoMapper;

namespace Arlo_chat.Api.Data.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<RegisterRequestModel, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore());
    }
}
