using AutoMapper;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Messages;

namespace PriceTrace.ApiService.Mappings;

public class ScraperMappingProfile : Profile
{
    public ScraperMappingProfile()
    {
        CreateMap<SearchRequestsEntity, StartScrapingCommand>()
            .ConstructUsing(src => new StartScrapingCommand(
                src.Id,
                src.Product.Id,
                src.User.Id,
                src.Product.ProductName,
                src.Product.ImageUrl ?? string.Empty,
                src.SoftUpLimit,
                src.SoftDownLimit,
                src.HardUpLimit,
                src.HardDownLimit,
                (int)src.FleaMarketWebEnum));
    }
}
