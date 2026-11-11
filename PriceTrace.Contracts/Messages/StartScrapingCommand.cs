namespace PriceTrace.Contracts.Messages;

public record StartScrapingCommand(
    Guid SearchRequestId,
    Guid ProductId,
    Guid UserId,
    string ProductName,
    int? SoftUpLimit,
    int? SoftDownLimit,
    int? HardUpLimit,
    int? HardDownLimit,
    int FleaMarketWeb);
