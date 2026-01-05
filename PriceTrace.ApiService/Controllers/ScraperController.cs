using Microsoft.AspNetCore.Mvc;
using PriceTrace.ApiService.Services;
using PriceTrace.Contracts.Messages;
using PriceTrace.DataEf.UnitOfWork;
using AutoMapper;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController(
    IUnitOfWork unitOfWork,
    IScraperCommandService scraperCommandService,
    IMapper mapper,
    ILogger<ScraperController> logger)
    : ControllerBase
{
    [HttpPost("start/{searchRequestId:guid}")]
    public async Task<IActionResult> StartScraping(Guid searchRequestId)
    {
        var searchRequest = await unitOfWork.SearchRequests.GetByIdAsync(searchRequestId);
        if (searchRequest is null)
            return NotFound($"SearchRequest with ID {searchRequestId} not found");

        var command = mapper.Map<StartScrapingCommand>(searchRequest);

        await scraperCommandService.StartScrapingAsync(command);

        logger.LogInformation("Started scraping for SearchRequestId: {SearchRequestId}", searchRequestId);
        return Ok(new { message = "Scraping started", searchRequestId });
    }

    [HttpPost("stop/{searchRequestId:guid}")]
    public async Task<IActionResult> StopScraping(Guid searchRequestId)
    {
        var searchRequest = await unitOfWork.SearchRequests.GetByIdAsync(searchRequestId);
        if (searchRequest is null)
            return NotFound($"SearchRequest with ID {searchRequestId} not found");

        var command = new StopScrapingCommand(searchRequestId);
        await scraperCommandService.StopScrapingAsync(command);

        logger.LogInformation("Stopped scraping for SearchRequestId: {SearchRequestId}", searchRequestId);
        return Ok(new { message = "Scraping stopped", searchRequestId });
    }
}
