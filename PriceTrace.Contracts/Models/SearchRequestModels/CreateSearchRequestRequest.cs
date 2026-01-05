using PriceTrace.Common.Enums;

namespace PriceTrace.Contracts.Models.SearchRequestModels;

// SearchRequest DTOs
public record CreateSearchRequestRequest(
    Guid UserId,
    Guid ProductId,
    FleaMarketWebEnum FleaMarketWeb,
    int? SoftUpLimit = null,
    int? SoftDownLimit = null,
    int? HardUpLimit = null,
    int? HardDownLimit = null,
    long IntervalInSeconds = 300);