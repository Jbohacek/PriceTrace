using MassTransit;
using PriceTrace.Contracts.Messages;
using PriceTrace.Application.Services;

namespace PriceTrace.ScraperService.Consumers;

public class StopScrapingCommandConsumer(
    ILogger<StopScrapingCommandConsumer> logger,
    IScrapingService scrapingService) : IConsumer<StopScrapingCommand>
{
    public async Task Consume(ConsumeContext<StopScrapingCommand> context)
    {
        var message = context.Message;
        
        logger.LogInformation(
            "Received stop scraping command for SearchRequestId: {SearchRequestId}",
            message.SearchRequestId);

        try
        {
            await scrapingService.StopScrapingAsync(message.SearchRequestId);
            logger.LogInformation("Scraping task stopped for SearchRequestId: {SearchRequestId}", message.SearchRequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping scraping for SearchRequestId: {SearchRequestId}", message.SearchRequestId);
            throw;
        }
    }
}
