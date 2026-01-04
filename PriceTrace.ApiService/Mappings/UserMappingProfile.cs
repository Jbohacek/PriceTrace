using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Users;

namespace PriceTrace.ApiService.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserEntity, UserResponse>()
            .ConstructUsing(src => new UserResponse(
                src.Id,
                src.Username,
                src.CreatedDate,
                src.UpdatedDate));

        CreateMap<CreateUserRequest, UserEntity>();
        CreateMap<UpdateUserRequest, UserEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
