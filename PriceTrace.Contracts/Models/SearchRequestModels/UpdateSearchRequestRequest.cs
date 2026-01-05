using PriceTrace.Common.Enums;

namespace PriceTrace.ApiService.Models;

public record UpdateSearchRequestRequest(
    FleaMarketWebEnum FleaMarketWeb,
    int? SoftUpLimit = null,
    int? SoftDownLimit = null,
    int? HardUpLimit = null,
    int? HardDownLimit = null,
    long IntervalInSeconds = 300);