using Microsoft.EntityFrameworkCore;
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

    public Task StartScrapingAsync(ScrapingTask scrapingTask)
    {
        if (!_runningTasks.TryAdd(scrapingTask.SearchRequestId, scrapingTask))
        {
            logger.LogWarning("Scraping task for SearchRequestId {SearchRequestId} is already running", 
                scrapingTask.SearchRequestId);
            return Task.CompletedTask;
        }

        logger.LogInformation("Starting scraping task for SearchRequestId {SearchRequestId} with interval {Interval}s",
            scrapingTask.SearchRequestId, scrapingTask.IntervalInSeconds);

        var task = Task.Run(async () => await ExecuteScrapingLoopAsync(scrapingTask), 
            scrapingTask.CancellationTokenSource.Token);

        scrapingTask.RunningTask = task;
        return Task.CompletedTask;
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

                var shouldContinue = await ExecuteSingleScrapingAsync(scrapingTask);
                if (!shouldContinue)
                {
                    _runningTasks.TryRemove(scrapingTask.SearchRequestId, out _);
                    break;
                }

                var delaySeconds = Math.Max(60, scrapingTask.IntervalInSeconds);
                logger.LogInformation("Scraping completed for SearchRequestId {SearchRequestId}. Waiting {Interval}s for next run",
                    scrapingTask.SearchRequestId, delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
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

    private async Task<bool> ExecuteSingleScrapingAsync(ScrapingTask scrapingTask)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var searchRequest = await unitOfWork.SearchRequests.GetByIdAsync(scrapingTask.SearchRequestId);
        if (searchRequest == null)
        {
            logger.LogInformation("SearchRequest {SearchRequestId} no longer exists in database. Stopping scraping loop.",
                scrapingTask.SearchRequestId);
            return false;
        }

        scrapingTask.IntervalInSeconds = searchRequest.IntervalInSeconds;
        scrapingTask.ProductName = searchRequest.Product.ProductName;
        scrapingTask.ProductNames = searchRequest.Product.Names;
        scrapingTask.HardUpLimit = searchRequest.HardUpLimit;
        scrapingTask.HardDownLimit = searchRequest.HardDownLimit;
        scrapingTask.SoftUpLimit = searchRequest.SoftUpLimit;
        scrapingTask.SoftDownLimit = searchRequest.SoftDownLimit;
        scrapingTask.FleaMarketWeb = (int)searchRequest.FleaMarketWebEnum;

        var page = await playwrightService.GetPageAsync();

        try
        {
            BaseScraper scraper = searchRequest.FleaMarketWebEnum switch
            {
                FleaMarketWebEnum.bazos => new BazosScraper(
                    scope.ServiceProvider.GetRequiredService<ILogger<BazosScraper>>()),
                _ => throw new NotSupportedException($"FleaMarketWeb type {searchRequest.FleaMarketWebEnum} is not supported")
            };

            var result = await scraper.GetSearchResult(page, searchRequest);

            logger.LogInformation("Scraping found {Count} listings for SearchRequestId {SearchRequestId}",
                result.Result.Count, scrapingTask.SearchRequestId);

            var product = searchRequest.Product;
            var scrapedUrls = result.Result.Select(l => l.Url).Distinct().ToList();

            var existingListingsByUrl = scrapedUrls.Count == 0
                ? new Dictionary<string, ListingEntity>()
                : await unitOfWork.Listings.Query()
                    .Where(l => l.Product.Id == product.Id && scrapedUrls.Contains(l.Url))
                    .ToDictionaryAsync(l => l.Url);

            int addedCount = 0;
            int updatedCount = 0;

            foreach (var listing in result.Result)
            {
                if (!existingListingsByUrl.TryGetValue(listing.Url, out var existingListing))
                {
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
                    existingListingsByUrl[listing.Url] = newListing;
                    addedCount++;

                    bool isOptimalDeal = listing.Price > 0 &&
                        (!searchRequest.SoftDownLimit.HasValue || listing.Price >= searchRequest.SoftDownLimit.Value) &&
                        (!searchRequest.SoftUpLimit.HasValue || listing.Price <= searchRequest.SoftUpLimit.Value) &&
                        (searchRequest.SoftDownLimit.HasValue || searchRequest.SoftUpLimit.HasValue);

                    logger.LogInformation(
                        "New Listing{DealTag}: {Title} | Price: {Price} Kč | Location: {Location} | URL: {Url}",
                        isOptimalDeal ? " [SOFT LIMIT MATCH]" : string.Empty,
                        listing.Title,
                        listing.Price,
                        listing.Location,
                        listing.Url);
                }
                else
                {
                    bool changed = existingListing.Price != listing.Price ||
                                   existingListing.Title != listing.Title ||
                                   existingListing.Description != listing.Description ||
                                   existingListing.Location != listing.Location;

                    existingListing.Price = listing.Price;
                    existingListing.Title = listing.Title;
                    existingListing.Description = listing.Description;
                    existingListing.Location = listing.Location;
                    existingListing.RetrievedAt = DateTime.UtcNow;

                    if (changed)
                    {
                        unitOfWork.Listings.Update(existingListing);
                        updatedCount++;
                    }
                }
            }

            if (addedCount > 0 || updatedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
                logger.LogInformation(
                    "Saved {AddedCount} new and {UpdatedCount} updated listings for SearchRequestId {SearchRequestId}", 
                    addedCount, updatedCount, scrapingTask.SearchRequestId);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing scraping for SearchRequestId {SearchRequestId}", 
                scrapingTask.SearchRequestId);
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
