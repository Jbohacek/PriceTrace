namespace PriceTrace.Contracts.Models.ListingModels;

public record FilteredListingsResponse(
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    IEnumerable<ListingResponse> Items);
