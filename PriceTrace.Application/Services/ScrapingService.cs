using Microsoft.Extensions.Logging;
using PriceTrace.Application.Scrapers;
using PriceTrace.Common.Enums;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Tasks;
using PriceTrace.DataEf.UnitOfWork;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace PriceTrace.Application.Services;

public class ScrapingService(
    ILogger<ScrapingService> logger,
    IPlaywrightService playwrightService,
    IServiceScopeFactory serviceScopeFactory) : IScrapingService
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
            
            await task.CancellationTokenSource.CancelAsync();
            
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
        using var scope = serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var page = await playwrightService.GetPageAsync();

        try
        {
            BaseScraper scraper = scrapingTask.FleaMarketWeb switch
            {
                (int)FleaMarketWebEnum.bazos => new BazosScraper(),
                _ => throw new NotSupportedException($"FleaMarketWeb type {scrapingTask.FleaMarketWeb} is not supported")
            };

            //todo: přepsat na mapper
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

            var result = await scraper.GetSearchResult(page, searchRequest);

            logger.LogInformation("Scraping found {Count} listings for SearchRequestId {SearchRequestId}",
                result.Result.Count, scrapingTask.SearchRequestId);

            var product = await unitOfWork.Products.GetByIdAsync(scrapingTask.ProductId);
            if (product == null)
            {
                logger.LogWarning("Product with Id {ProductId} not found", scrapingTask.ProductId);
                return;
            }

            int addedCount = 0;
            int skippedCount = 0;

            foreach (var listing in result.Result)
            {
                var existingListing = await unitOfWork.Listings.FirstOrDefaultAsync(l => l.Url == listing.Url);
                
                if (existingListing == null)
                {
                    //todo: přepsat na mapper
                    var newListing = new ListingEntity
                    {
                        Product = product,
                        Url = listing.Url,
                        Price = listing.Price,
                        Title = listing.Title,
                        Description = listing.Description,
                        Location = listing.Location,
                        CreatorName = listing.CreatorName,
                        ViewCount = listing.ViewCount,
                        RetrievedAt = DateTime.UtcNow
                    };

                    await unitOfWork.Listings.AddAsync(newListing);
                    addedCount++;

                    logger.LogInformation(
                        "New Listing: {Title} | Price: {Price} Kč | Location: {Location} | URL: {Url}",
                        listing.Title,
                        listing.Price,
                        listing.Location,
                        listing.Url);
                }
                else
                {
                    skippedCount++;
                    logger.LogDebug("Listing already exists: {Url}", listing.Url);
                }
            }

            if (addedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
                logger.LogInformation("Saved {AddedCount} new listings to database for SearchRequestId {SearchRequestId}", 
                    addedCount, scrapingTask.SearchRequestId);
            }

            if (skippedCount > 0)
            {
                logger.LogInformation("Skipped {SkippedCount} duplicate listings for SearchRequestId {SearchRequestId}", 
                    skippedCount, scrapingTask.SearchRequestId);
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
