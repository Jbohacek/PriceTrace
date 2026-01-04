using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Products;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ListingsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListingResponse>>> GetAll()
    {
        var listings = await _unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .ToListAsync();
        
        return Ok(_mapper.Map<IEnumerable<ListingResponse>>(listings));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> GetById(Guid id)
    {
        var listing = await _unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return NotFound();

        return Ok(_mapper.Map<ListingResponse>(listing));
    }

    [HttpPost]
    public async Task<ActionResult<ListingResponse>> Create([FromBody] CreateListingRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product is null)
            return BadRequest($"Product with ID {request.ProductId} not found.");

        var listing = _mapper.Map<ListingEntity>(request);
        listing.Product = product;

        await _unitOfWork.Listings.AddAsync(listing);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<ListingResponse>(listing);
        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> Update(Guid id, [FromBody] UpdateListingRequest request)
    {
        var listing = await _unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return NotFound();

        if (request.ProductId.HasValue && request.ProductId.Value != listing.Product.Id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId.Value);
            if (product is null)
                return BadRequest($"Product with ID {request.ProductId.Value} not found.");
            
            listing.Product = product;
        }

        _mapper.Map(request, listing);
        listing.UpdatedDate = DateTime.Now;

        _unitOfWork.Listings.Update(listing);
        await _unitOfWork.SaveChangesAsync();

        return Ok(_mapper.Map<ListingResponse>(listing));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var listing = await _unitOfWork.Listings.GetByIdAsync(id);
        if (listing is null)
            return NotFound();

        _unitOfWork.Listings.Remove(listing);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("filter")]
    public async Task<ActionResult<object>> Filter([FromBody] ListingFilterRequest filter)
    {
        var query = _unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .AsQueryable();

        // Apply filters
        if (filter.ProductId.HasValue)
            query = query.Where(l => l.Product.Id == filter.ProductId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(l => l.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(l => l.Price <= filter.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(l => l.Location != null && l.Location.Contains(filter.Location));

        if (!string.IsNullOrWhiteSpace(filter.CreatorName))
            query = query.Where(l => l.CreatorName != null && l.CreatorName.Contains(filter.CreatorName));

        if (filter.RetrievedAfter.HasValue)
            query = query.Where(l => l.RetrievedAt >= filter.RetrievedAfter.Value);

        if (filter.RetrievedBefore.HasValue)
            query = query.Where(l => l.RetrievedAt <= filter.RetrievedBefore.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            query = query.Where(l => 
                l.Title.Contains(filter.SearchTerm) ||
                (l.Description != null && l.Description.Contains(filter.SearchTerm)));
        }

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "price" => filter.SortDescending 
                ? query.OrderByDescending(l => l.Price) 
                : query.OrderBy(l => l.Price),
            "title" => filter.SortDescending 
                ? query.OrderByDescending(l => l.Title) 
                : query.OrderBy(l => l.Title),
            "retrievedat" => filter.SortDescending 
                ? query.OrderByDescending(l => l.RetrievedAt) 
                : query.OrderBy(l => l.RetrievedAt),
            "location" => filter.SortDescending 
                ? query.OrderByDescending(l => l.Location) 
                : query.OrderBy(l => l.Location),
            "viewcount" => filter.SortDescending 
                ? query.OrderByDescending(l => l.ViewCount) 
                : query.OrderBy(l => l.ViewCount),
            _ => filter.SortDescending 
                ? query.OrderByDescending(l => l.CreatedDate) 
                : query.OrderBy(l => l.CreatedDate)
        };

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 50 : filter.PageSize > 100 ? 100 : filter.PageSize;
        
        var listings = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = _mapper.Map<IEnumerable<ListingResponse>>(listings)
        };

        return Ok(result);
    }
}
