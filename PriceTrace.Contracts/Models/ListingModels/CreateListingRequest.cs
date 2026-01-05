namespace PriceTrace.Contracts.Models.ListingModels;

public record CreateListingRequest(
    Guid ProductId,
    string Url,
    decimal Price,
    string Title,
    string? Description = null,
    string? Location = null,
    string? CreatorName = null,
    int? ViewCount = null);