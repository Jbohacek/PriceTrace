namespace PriceTrace.Contracts.Messages;

public record StopScrapingCommand(
    Guid SearchRequestId);
