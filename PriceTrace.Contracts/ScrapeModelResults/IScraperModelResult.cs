using PriceTrace.Common.Products;

namespace PriceTrace.Contracts.ScrapeModelResults;

public interface IScraperModelResult
{
    public List<ListingEntity> Result { get; set; }
}
