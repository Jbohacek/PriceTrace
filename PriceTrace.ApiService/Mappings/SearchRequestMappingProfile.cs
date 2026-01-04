using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Users;

namespace PriceTrace.ApiService.Mappings;

public class SearchRequestMappingProfile : Profile
{
    public SearchRequestMappingProfile()
    {
        CreateMap<SearchRequestsEntity, SearchRequestResponse>()
            .ConstructUsing(src => new SearchRequestResponse(
                src.Id,
                src.User.Id,
                src.Product.Id,
                src.FleaMarketWebEnum,
                src.SoftUpLimit,
                src.SoftDownLimit,
                src.HardUpLimit,
                src.HardDownLimit,
                src.IntervalInSeconds,
                src.CreatedDate,
                src.UpdatedDate));

        CreateMap<CreateSearchRequestRequest, SearchRequestsEntity>()
            .ForMember(dest => dest.FleaMarketWebEnum, opt => opt.MapFrom(src => src.FleaMarketWeb))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        CreateMap<UpdateSearchRequestRequest, SearchRequestsEntity>()
            .ForMember(dest => dest.FleaMarketWebEnum, opt => opt.MapFrom(src => src.FleaMarketWeb))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
