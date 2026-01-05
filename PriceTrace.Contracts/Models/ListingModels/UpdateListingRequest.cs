namespace PriceTrace.Contracts.Models.ListingModels;

public record UpdateListingRequest(
    Guid? ProductId = null,
    string? Url = null,
    decimal? Price = null,
    string? Title = null,
    string? Description = null,
    string? Location = null,
    string? CreatorName = null,
    int? ViewCount = null);