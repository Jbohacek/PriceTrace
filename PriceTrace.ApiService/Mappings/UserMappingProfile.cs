using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Users;

namespace PriceTrace.ApiService.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity -> Response
        CreateMap<UserEntity, UserResponse>();

        // Request -> Entity
        CreateMap<CreateUserRequest, UserEntity>();
        CreateMap<UpdateUserRequest, UserEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
