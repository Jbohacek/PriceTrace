using Microsoft.Extensions.Logging;
using PriceTrace.Application.Scrapers;
using PriceTrace.Common.Enums;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Tasks;
using System.Collections.Concurrent;

namespace PriceTrace.Application.Services;

public class ScrapingService(
    ILogger<ScrapingService> logger,
    IPlaywrightService playwrightService) : IScrapingService
{
    private readonly ConcurrentDictionary<Guid, ScrapingTask> _runningTasks = new();

    public bool IsRunning(Guid searchRequestId)
    {
        return _runningTasks.ContainsKey(searchRequestId);
    }

    public async Task StartScrapingAsync(ScrapingTask scrapingTask)
    {
        if (_runningTasks.ContainsKey(scrapingTask.SearchRequestId))
        {
            logger.LogWarning("Scraping task for SearchRequestId {SearchRequestId} is already running", 
                scrapingTask.SearchRequestId);
            return;
        }

        logger.LogInformation("Starting scraping task for SearchRequestId {SearchRequestId} with interval {Interval}s",
            scrapingTask.SearchRequestId, scrapingTask.IntervalInSeconds);

        var task = Task.Run(async () => await ExecuteScrapingLoopAsync(scrapingTask), 
            scrapingTask.CancellationTokenSource.Token);

        scrapingTask.RunningTask = task;
        _runningTasks.TryAdd(scrapingTask.SearchRequestId, scrapingTask);
    }

    public async Task StopScrapingAsync(Guid searchRequestId)
    {
        if (_runningTasks.TryRemove(searchRequestId, out var task))
        {
            logger.LogInformation("Stopping scraping task for SearchRequestId {SearchRequestId}", searchRequestId);
            
            task.CancellationTokenSource.Cancel();
            
            if (task.RunningTask != null)
            {
                try
                {
                    await task.RunningTask;
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Scraping task for SearchRequestId {SearchRequestId} was cancelled", 
                        searchRequestId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while stopping scraping task for SearchRequestId {SearchRequestId}", 
                        searchRequestId);
                }
            }

            task.CancellationTokenSource.Dispose();
        }
        else
        {
            logger.LogWarning("No running scraping task found for SearchRequestId {SearchRequestId}", searchRequestId);
        }
    }

    private async Task ExecuteScrapingLoopAsync(ScrapingTask scrapingTask)
    {
        var cancellationToken = scrapingTask.CancellationTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Executing scraping for SearchRequestId {SearchRequestId}, Product: {ProductName}",
                    scrapingTask.SearchRequestId, scrapingTask.ProductName);

                await ExecuteSingleScrapingAsync(scrapingTask);

                logger.LogInformation("Scraping completed for SearchRequestId {SearchRequestId}. Waiting {Interval}s for next run",
                    scrapingTask.SearchRequestId, scrapingTask.IntervalInSeconds);

                await Task.Delay(TimeSpan.FromSeconds(scrapingTask.IntervalInSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Scraping loop cancelled for SearchRequestId {SearchRequestId}", 
                    scrapingTask.SearchRequestId);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during scraping for SearchRequestId {SearchRequestId}", 
                    scrapingTask.SearchRequestId);

                // Pokud dojde k chybì, poèkáme kratší dobu pøed dalším pokusem
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ExecuteSingleScrapingAsync(ScrapingTask scrapingTask)
    {
        // Získání page z PlaywrightService (shared browser)
        var page = await playwrightService.GetPageAsync();

        try
        {
            // Vytvoøení scraperu podle typu tržištì
            BaseScraper scraper = scrapingTask.FleaMarketWeb switch
            {
                (int)FleaMarketWebEnum.bazos => new BazosScraper(),
                _ => throw new NotSupportedException($"FleaMarketWeb type {scrapingTask.FleaMarketWeb} is not supported")
            };

            // Vytvoøení SearchRequestsEntity pro scraper
            var searchRequest = new SearchRequestsEntity
            {
                Id = scrapingTask.SearchRequestId,
                FleaMarketWebEnum = (FleaMarketWebEnum)scrapingTask.FleaMarketWeb,
                HardUpLimit = scrapingTask.HardUpLimit,
                HardDownLimit = scrapingTask.HardDownLimit,
                SoftUpLimit = scrapingTask.SoftUpLimit,
                SoftDownLimit = scrapingTask.SoftDownLimit,
                IntervalInSeconds = scrapingTask.IntervalInSeconds,
                Product = new ProductEntity
                {
                    Id = scrapingTask.ProductId,
                    ProductName = scrapingTask.ProductName,
                    Names = scrapingTask.ProductNames,
                    ProductDescription = string.Empty
                },
                User = new UserEntity
                {
                    Id = scrapingTask.UserId,
                    Username = string.Empty
                }
            };

            // Spuštìní scrapingu
            var result = await scraper.GetSearchResult(page, searchRequest);

            logger.LogInformation("Scraping found {Count} listings for SearchRequestId {SearchRequestId}",
                result.Result.Count, scrapingTask.SearchRequestId);

            // Výpis výsledkù do konzole
            foreach (var listing in result.Result)
            {
                logger.LogInformation(
                    "Listing: {Title} | Price: {Price} Kè | Location: {Location} | URL: {Url}",
                    listing.Title,
                    listing.Price,
                    listing.Location,
                    listing.Url);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing scraping for SearchRequestId {SearchRequestId}", 
                scrapingTask.SearchRequestId);
            throw;
        }
    }
}
