using PriceTrace.Common.Products;

namespace PriceTrace.Contracts.ScapeModelResults;

public interface IScaperModelResult
{
    public List<ListingEntity> Result { get; set; }
}