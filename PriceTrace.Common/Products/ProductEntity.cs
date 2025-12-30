using PriceTrace.Common.Core;
using PriceTrace.Common.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTrace.Common.Products;

[Table("tbProduct", Schema = "Products")]
public class ProductEntity : BaseEntity
{
    public string[] Names { get; set; } = Array.Empty<string>();

    public string? ImageUrl { get; set; }

    public required string ProductName { get; set; }

    public required string ProductDescription { get; set; }

    public List<ListingEntity> Listings { get; set; } = new();

    public List<EshopPriceEntity> EshopPrices { get; set; } = new();

    public List<SearchRequestsEntity> SearchRequests { get; set; } = new();
}