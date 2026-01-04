using Microsoft.AspNetCore.Mvc;
using PriceTrace.ApiService.Services;
using PriceTrace.Contracts.Messages;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScraperCommandService _scraperCommandService;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        IUnitOfWork unitOfWork,
        IScraperCommandService scraperCommandService,
        ILogger<ScraperController> logger)
    {
        _unitOfWork = unitOfWork;
        _scraperCommandService = scraperCommandService;
        _logger = logger;
    }

    [HttpPost("start/{searchRequestId:guid}")]
    public async Task<IActionResult> StartScraping(Guid searchRequestId)
    {
        var searchRequest = await _unitOfWork.SearchRequests.GetByIdAsync(searchRequestId);
        if (searchRequest is null)
            return NotFound($"SearchRequest with ID {searchRequestId} not found");

        var command = new StartScrapingCommand(
            searchRequest.Id,
            searchRequest.Product.Id,
            searchRequest.User.Id,
            searchRequest.Product.ProductName,
            searchRequest.Product.ImageUrl ?? string.Empty,
            searchRequest.SoftUpLimit,
            searchRequest.SoftDownLimit,
            searchRequest.HardUpLimit,
            searchRequest.HardDownLimit,
            (int)searchRequest.FleaMarketWebEnum);

        await _scraperCommandService.StartScrapingAsync(command);

        _logger.LogInformation("Started scraping for SearchRequestId: {SearchRequestId}", searchRequestId);
        return Ok(new { message = "Scraping started", searchRequestId });
    }

    [HttpPost("stop/{searchRequestId:guid}")]
    public async Task<IActionResult> StopScraping(Guid searchRequestId)
    {
        var searchRequest = await _unitOfWork.SearchRequests.GetByIdAsync(searchRequestId);
        if (searchRequest is null)
            return NotFound($"SearchRequest with ID {searchRequestId} not found");

        var command = new StopScrapingCommand(searchRequestId);
        await _scraperCommandService.StopScrapingAsync(command);

        _logger.LogInformation("Stopped scraping for SearchRequestId: {SearchRequestId}", searchRequestId);
        return Ok(new { message = "Scraping stopped", searchRequestId });
    }
}
