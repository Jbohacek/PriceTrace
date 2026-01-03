using PriceTrace.Common.Enums;

namespace PriceTrace.ApiService.Models;

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

public record UpdateSearchRequestRequest(
    FleaMarketWebEnum FleaMarketWeb,
    int? SoftUpLimit = null,
    int? SoftDownLimit = null,
    int? HardUpLimit = null,
    int? HardDownLimit = null,
    long IntervalInSeconds = 300);

public record SearchRequestResponse(
    Guid Id,
    Guid UserId,
    Guid ProductId,
    FleaMarketWebEnum FleaMarketWeb,
    int? SoftUpLimit,
    int? SoftDownLimit,
    int? HardUpLimit,
    int? HardDownLimit,
    long IntervalInSeconds,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
