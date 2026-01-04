using PriceTrace.Contracts.Tasks;

namespace PriceTrace.Application.Services;

public interface IScrapingService
{
    Task StartScrapingAsync(ScrapingTask scrapingTask);
    Task StopScrapingAsync(Guid searchRequestId);
    bool IsRunning(Guid searchRequestId);
}