using PriceTrace.Common.Core;
using PriceTrace.Common.Enums;

namespace PriceTrace.Common.Products;

public class ListingEntity : BaseEntity
{
    public required ProductEntity Product { get; set; }

    public required FleaMarketWebEnum FleaMarketWebEnum { get; set; }

    public required string Url { get; set; }

    /// <summary>
    /// -1 = Dohodou
    /// 0 = Zdarma
    /// 1+ = Cena v Kč
    /// </summary>
    public required decimal Price { get; set; } = 0;

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    public string? Location { get; set; }

    public string? CreatorName { get; set; }

    public int? ViewCount { get; set; }
}