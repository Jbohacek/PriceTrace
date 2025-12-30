using PriceTrace.Common.Core;

namespace PriceTrace.Common.Products;

public class EshopPriceEntity : BaseEntity
{
    public decimal Price { get; set; }
    public required string Url { get; set; }
    public required ProductEntity Product { get; set; }
}