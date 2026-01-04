using AutoMapper;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Products;

namespace PriceTrace.ApiService.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<ProductEntity, ProductResponse>()
            .ConstructUsing(src => new ProductResponse(
                src.Id,
                src.ProductName,
                src.ProductDescription,
                src.Names,
                src.ImageUrl,
                src.CreatedDate,
                src.UpdatedDate));

        CreateMap<CreateProductRequest, ProductEntity>();
        CreateMap<UpdateProductRequest, ProductEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
