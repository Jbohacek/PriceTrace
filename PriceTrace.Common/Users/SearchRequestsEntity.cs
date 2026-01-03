using PriceTrace.Common.Core;
using PriceTrace.Common.Enums;
using PriceTrace.Common.Products;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTrace.Common.Users;

[Table("tbSearchRequest", Schema = "User")]
public class SearchRequestsEntity : BaseEntity
{

    public int? SoftUpLimit { get; set; }
    public int? SoftDownLimit { get; set; }

    public int? HardUpLimit { get; set; }
    public int? HardDownLimit { get; set; }

    public long IntervalInSeconds { get; set; } = 300;

    public required FleaMarketWebEnum FleaMarketWebEnum { get; set; }

    public required UserEntity User { get; set; }
    public required ProductEntity Product { get; set; }
}