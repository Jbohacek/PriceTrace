using PriceTrace.Common.Enums;

namespace PriceTrace.Contracts.Models.SearchRequestModels;

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