using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTrace.Common.Products;
using PriceTrace.Contracts.Models.ListingModels;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController(IUnitOfWork unitOfWork, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListingResponse>>> GetAll()
    {
        var listings = await unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .ToListAsync();
        
        return Ok(mapper.Map<IEnumerable<ListingResponse>>(listings));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> GetById(Guid id)
    {
        var listing = await unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return NotFound();

        return Ok(mapper.Map<ListingResponse>(listing));
    }

    [HttpPost]
    public async Task<ActionResult<ListingResponse>> Create([FromBody] CreateListingRequest request)
    {
        var product = await unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product is null)
            return BadRequest($"Product with ID {request.ProductId} not found.");

        var listing = mapper.Map<ListingEntity>(request);
        listing.Product = product;

        await unitOfWork.Listings.AddAsync(listing);
        await unitOfWork.SaveChangesAsync();

        var response = mapper.Map<ListingResponse>(listing);
        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> Update(Guid id, [FromBody] UpdateListingRequest request)
    {
        var listing = await unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return NotFound();

        if (request.ProductId.HasValue && request.ProductId.Value != listing.Product.Id)
        {
            var product = await unitOfWork.Products.GetByIdAsync(request.ProductId.Value);
            if (product is null)
                return BadRequest($"Product with ID {request.ProductId.Value} not found.");
            
            listing.Product = product;
        }

        mapper.Map(request, listing);

        unitOfWork.Listings.Update(listing);
        await unitOfWork.SaveChangesAsync();

        return Ok(mapper.Map<ListingResponse>(listing));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var listing = await unitOfWork.Listings.GetByIdAsync(id);
        if (listing is null)
            return NotFound();

        unitOfWork.Listings.Remove(listing);
        await unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("filter")]
    public async Task<ActionResult<FilteredListingsResponse>> Filter([FromBody] ListingFilterRequest filter)
    {
        var query = unitOfWork.Listings.Query()
            .Include(l => l.Product)
            .AsQueryable();

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

        var totalCount = await query.CountAsync();

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 50 : filter.PageSize > 100 ? 100 : filter.PageSize;
        
        var listings = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new FilteredListingsResponse(
            totalCount,
            pageNumber,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            mapper.Map<IEnumerable<ListingResponse>>(listings));

        return Ok(result);
    }
}
