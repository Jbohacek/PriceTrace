using PriceTrace.Common.Products;

namespace PriceTrace.Contracts.ScrapeModelResults;

public class BazosScraperResult : IScraperModelResult
{
    public required List<ListingEntity> Result { get; set; }
}
