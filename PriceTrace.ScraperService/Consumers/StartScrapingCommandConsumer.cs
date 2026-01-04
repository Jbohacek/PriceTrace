using MassTransit;
using PriceTrace.Contracts.Messages;

namespace PriceTrace.ScraperService.Consumers;

public class StartScrapingCommandConsumer(ILogger<StartScrapingCommandConsumer> logger) : IConsumer<StartScrapingCommand>
{
    public async Task Consume(ConsumeContext<StartScrapingCommand> context)
    {
        var message = context.Message;
        
        logger.LogInformation(
            "Received scraping command for SearchRequestId: {SearchRequestId}, Product: {ProductName}",
            message.SearchRequestId,
            message.ProductName);

        // TODO: Implement actual scraping logic here
        // You can inject your scraper service here and call it
        
        await Task.CompletedTask;
    }
}
