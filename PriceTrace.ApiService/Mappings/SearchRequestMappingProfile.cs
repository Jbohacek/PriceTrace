using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Users;

namespace PriceTrace.ApiService.Mappings;

public class SearchRequestMappingProfile : Profile
{
    public SearchRequestMappingProfile()
    {
        // Entity -> Response
        CreateMap<SearchRequestsEntity, SearchRequestResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Product.Id))
            .ForMember(dest => dest.FleaMarketWeb, opt => opt.MapFrom(src => src.FleaMarketWebEnum));

        // Request -> Entity (partial - User and Product must be set manually)
        CreateMap<CreateSearchRequestRequest, SearchRequestsEntity>()
            .ForMember(dest => dest.FleaMarketWebEnum, opt => opt.MapFrom(src => src.FleaMarketWeb))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        CreateMap<UpdateSearchRequestRequest, SearchRequestsEntity>()
            .ForMember(dest => dest.FleaMarketWebEnum, opt => opt.MapFrom(src => src.FleaMarketWeb))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
