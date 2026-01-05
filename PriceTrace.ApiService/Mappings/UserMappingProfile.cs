using AutoMapper;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Models.UserModels;

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
