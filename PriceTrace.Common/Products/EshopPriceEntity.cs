using PriceTrace.Common.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTrace.Common.Products;

[Table("tbEshopPrice", Schema = "Products")]
public class EshopPriceEntity : BaseEntity
{
    public decimal Price { get; set; }
    public required string Url { get; set; }
    public required ProductEntity Product { get; set; }
}