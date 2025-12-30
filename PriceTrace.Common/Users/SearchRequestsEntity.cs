using PriceTrace.Common.Core;
using PriceTrace.Common.Products;

namespace PriceTrace.Common.Users;

public class SearchRequestsEntity : BaseEntity
{

    public int? SoftUpLimit { get; set; }
    public int? SoftDownLimit { get; set; }

    public int? HardUpLimit { get; set; }
    public int? HardDownLimit { get; set; }

    public long IntervalInSeconds { get; set; } = 300;

    public required UserEntity User { get; set; }
    public required ProductEntity Product { get; set; }
}