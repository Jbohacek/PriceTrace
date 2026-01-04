namespace PriceTrace.ApiService.Models;

// Listing DTOs
public record CreateListingRequest(
    Guid ProductId,
    string Url,
    decimal Price,
    string Title,
    string? Description = null,
    string? Location = null,
    string? CreatorName = null,
    int? ViewCount = null);

public record UpdateListingRequest(
    Guid? ProductId = null,
    string? Url = null,
    decimal? Price = null,
    string? Title = null,
    string? Description = null,
    string? Location = null,
    string? CreatorName = null,
    int? ViewCount = null);

public record ListingResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Url,
    decimal Price,
    string Title,
    string? Description,
    DateTime RetrievedAt,
    string? Location,
    string? CreatorName,
    int? ViewCount,
    DateTime CreatedDate,
    DateTime? UpdatedDate);

public record ListingFilterRequest(
    Guid? ProductId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Location = null,
    string? CreatorName = null,
    DateTime? RetrievedAfter = null,
    DateTime? RetrievedBefore = null,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false,
    int PageNumber = 1,
    int PageSize = 50);
