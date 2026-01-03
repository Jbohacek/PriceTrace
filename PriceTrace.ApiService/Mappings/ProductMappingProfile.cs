using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Products;

namespace PriceTrace.ApiService.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // Entity -> Response
        CreateMap<ProductEntity, ProductResponse>();

        // Request -> Entity
        CreateMap<CreateProductRequest, ProductEntity>();
        CreateMap<UpdateProductRequest, ProductEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
