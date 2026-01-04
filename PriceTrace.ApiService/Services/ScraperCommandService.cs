using MassTransit;
using PriceTrace.Contracts.Messages;

namespace PriceTrace.ApiService.Services;

public interface IScraperCommandService
{
    Task StartScrapingAsync(StartScrapingCommand command, CancellationToken cancellationToken = default);
    Task StopScrapingAsync(StopScrapingCommand command, CancellationToken cancellationToken = default);
}

public class ScraperCommandService(IPublishEndpoint publishEndpoint, ILogger<ScraperCommandService> logger) : IScraperCommandService
{
    public async Task StartScrapingAsync(StartScrapingCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Publishing StartScrapingCommand for SearchRequestId: {SearchRequestId}", command.SearchRequestId);
        await publishEndpoint.Publish(command, cancellationToken);
    }

    public async Task StopScrapingAsync(StopScrapingCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Publishing StopScrapingCommand for SearchRequestId: {SearchRequestId}", command.SearchRequestId);
        await publishEndpoint.Publish(command, cancellationToken);
    }
}
