using MassTransit;
using PriceTrace.Contracts.Messages;
using PriceTrace.Contracts.Tasks;
using PriceTrace.DataEf.UnitOfWork;
using PriceTrace.Application.Services;

namespace PriceTrace.ScraperService.Consumers;

public class StartScrapingCommandConsumer(
    ILogger<StartScrapingCommandConsumer> logger,
    IScrapingService scrapingService,
    IUnitOfWork unitOfWork) : IConsumer<StartScrapingCommand>
{
    public async Task Consume(ConsumeContext<StartScrapingCommand> context)
    {
        var message = context.Message;
        
        logger.LogInformation(
            "Received scraping command for SearchRequestId: {SearchRequestId}, Product: {ProductName}",
            message.SearchRequestId,
            message.ProductName);

        try
        {
            // Načtení SearchRequest z databáze pro získání IntervalInSeconds a ProductNames
            var searchRequest = await unitOfWork.SearchRequests.GetByIdAsync(message.SearchRequestId);
            
            if (searchRequest == null)
            {
                logger.LogWarning("SearchRequest {SearchRequestId} not found in database", message.SearchRequestId);
                return;
            }

            var scrapingTask = new ScrapingTask
            {
                SearchRequestId = message.SearchRequestId,
                ProductId = message.ProductId,
                UserId = message.UserId,
                ProductName = message.ProductName,
                ProductNames = searchRequest.Product.Names,
                SoftUpLimit = message.SoftUpLimit,
                SoftDownLimit = message.SoftDownLimit,
                HardUpLimit = message.HardUpLimit,
                HardDownLimit = message.HardDownLimit,
                FleaMarketWeb = message.FleaMarketWeb,
                IntervalInSeconds = searchRequest.IntervalInSeconds
            };

            await scrapingService.StartScrapingAsync(scrapingTask);
            
            logger.LogInformation("Scraping task started for SearchRequestId: {SearchRequestId}", message.SearchRequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting scraping for SearchRequestId: {SearchRequestId}", message.SearchRequestId);
        }
    }
}
