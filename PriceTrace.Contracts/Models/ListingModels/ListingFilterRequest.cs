namespace PriceTrace.Contracts.Models.ListingModels;

// Listing DTOs

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
