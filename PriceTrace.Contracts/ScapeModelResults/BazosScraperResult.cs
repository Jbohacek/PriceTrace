using PriceTrace.Common.Products;

namespace PriceTrace.Contracts.ScapeModelResults;

public class BazosScraperResult : IScaperModelResult
{
    public required List<ListingEntity> Result { get; set; }
}