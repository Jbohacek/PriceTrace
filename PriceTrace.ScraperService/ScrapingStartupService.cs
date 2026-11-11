using PriceTrace.Application.Services;
using PriceTrace.Contracts.Tasks;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ScraperService;

public class ScrapingStartupService(
    ILogger<ScrapingStartupService> logger,
    IServiceScopeFactory serviceScopeFactory,
    IScrapingService scrapingService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var searchRequests = (await unitOfWork.SearchRequests.GetAllAsync(cancellationToken)).ToList();
            logger.LogInformation("Resuming {Count} existing search requests from database on startup", searchRequests.Count);

            foreach (var sr in searchRequests)
            {
                var scrapingTask = new ScrapingTask
                {
                    SearchRequestId = sr.Id,
                    ProductId = sr.Product.Id,
                    UserId = sr.User.Id,
                    ProductName = sr.Product.ProductName,
                    ProductNames = sr.Product.Names,
                    SoftUpLimit = sr.SoftUpLimit,
                    SoftDownLimit = sr.SoftDownLimit,
                    HardUpLimit = sr.HardUpLimit,
                    HardDownLimit = sr.HardDownLimit,
                    FleaMarketWeb = (int)sr.FleaMarketWebEnum,
                    IntervalInSeconds = sr.IntervalInSeconds
                };

                await scrapingService.StartScrapingAsync(scrapingTask);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while resuming search requests from database on startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
