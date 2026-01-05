using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PriceTrace.ApiService.Models;
using PriceTrace.ApiService.Services;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Messages;
using PriceTrace.Contracts.Models.SearchRequestModels;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchRequestsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IScraperCommandService _scraperCommandService;

    public SearchRequestsController(IUnitOfWork unitOfWork, IMapper mapper, IScraperCommandService scraperCommandService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _scraperCommandService = scraperCommandService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchRequestResponse>>> GetAll()
    {
        var requests = await _unitOfWork.SearchRequests.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<SearchRequestResponse>>(requests));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SearchRequestResponse>> GetById(Guid id)
    {
        var request = await _unitOfWork.SearchRequests.GetByIdAsync(id);
        if (request is null)
            return NotFound();

        return Ok(_mapper.Map<SearchRequestResponse>(request));
    }

    [HttpGet("by-user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<SearchRequestResponse>>> GetByUserId(Guid userId)
    {
        var requests = await _unitOfWork.SearchRequests.FindAsync(r => r.User.Id == userId);
        return Ok(_mapper.Map<IEnumerable<SearchRequestResponse>>(requests));
    }

    [HttpPost]
    public async Task<ActionResult<SearchRequestResponse>> Create([FromBody] CreateSearchRequestRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return NotFound($"User with ID {request.UserId} not found");

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product is null)
            return NotFound($"Product with ID {request.ProductId} not found");

        var searchRequest = _mapper.Map<SearchRequestsEntity>(request);
        searchRequest.User = user;
        searchRequest.Product = product;

        await _unitOfWork.SearchRequests.AddAsync(searchRequest);
        await _unitOfWork.SaveChangesAsync();

        var startCommand = _mapper.Map<StartScrapingCommand>(searchRequest);

        await _scraperCommandService.StartScrapingAsync(startCommand);

        var response = _mapper.Map<SearchRequestResponse>(searchRequest);
        return CreatedAtAction(nameof(GetById), new { id = searchRequest.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SearchRequestResponse>> Update(Guid id, [FromBody] UpdateSearchRequestRequest request)
    {
        var searchRequest = await _unitOfWork.SearchRequests.GetByIdAsync(id);
        if (searchRequest is null)
            return NotFound();

        _mapper.Map(request, searchRequest);
        searchRequest.UpdatedDate = DateTime.Now;

        _unitOfWork.SearchRequests.Update(searchRequest);
        await _unitOfWork.SaveChangesAsync();

        return Ok(_mapper.Map<SearchRequestResponse>(searchRequest));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var searchRequest = await _unitOfWork.SearchRequests.GetByIdAsync(id);
        if (searchRequest is null)
            return NotFound();

        _unitOfWork.SearchRequests.Remove(searchRequest);
        await _unitOfWork.SaveChangesAsync();

        var stopCommand = new StopScrapingCommand(id);
        await _scraperCommandService.StopScrapingAsync(stopCommand);

        return NoContent();
    }
}
