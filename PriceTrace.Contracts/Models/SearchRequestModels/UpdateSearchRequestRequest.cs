using PriceTrace.Common.Enums;

namespace PriceTrace.Contracts.Models.SearchRequestModels;

public record UpdateSearchRequestRequest(
    FleaMarketWebEnum FleaMarketWeb,
    int? SoftUpLimit = null,
    int? SoftDownLimit = null,
    int? HardUpLimit = null,
    int? HardDownLimit = null,
    long IntervalInSeconds = 300);