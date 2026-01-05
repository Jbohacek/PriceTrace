using AutoMapper;
using PriceTrace.Common.Products;
using PriceTrace.Contracts.Models.ListingModels;

namespace PriceTrace.ApiService.Mappings;

public class ListingMappingProfile : Profile
{
    public ListingMappingProfile()
    {
        CreateMap<ListingEntity, ListingResponse>()
            .ConstructUsing(src => new ListingResponse(
                src.Id,
                src.Product.Id,
                src.Product.ProductName,
                src.Url,
                src.Price,
                src.Title,
                src.Description,
                src.RetrievedAt,
                src.Location,
                src.CreatorName,
                src.ViewCount,
                src.CreatedDate,
                src.UpdatedDate));

        CreateMap<CreateListingRequest, ListingEntity>()
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        CreateMap<UpdateListingRequest, ListingEntity>()
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
