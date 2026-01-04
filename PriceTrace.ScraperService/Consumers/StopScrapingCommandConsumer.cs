using MassTransit;
using PriceTrace.Contracts.Messages;

namespace PriceTrace.ScraperService.Consumers;

public class StopScrapingCommandConsumer(ILogger<StopScrapingCommandConsumer> logger) : IConsumer<StopScrapingCommand>
{
    public async Task Consume(ConsumeContext<StopScrapingCommand> context)
    {
        var message = context.Message;
        
        logger.LogInformation(
            "Received stop scraping command for SearchRequestId: {SearchRequestId}",
            message.SearchRequestId);

        // TODO: Implement logic to stop scraping for this SearchRequestId
        
        await Task.CompletedTask;
    }
}
