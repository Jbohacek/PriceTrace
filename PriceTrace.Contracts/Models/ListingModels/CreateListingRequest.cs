using System.ComponentModel.DataAnnotations;

namespace PriceTrace.Contracts.Models.ListingModels;

public record CreateListingRequest(
    Guid ProductId,
    [property: Required(AllowEmptyStrings = false)] string Url,
    [property: Range(-1, double.MaxValue)] decimal Price,
    [property: Required(AllowEmptyStrings = false)] string Title,
    string? Description = null,
    string? Location = null,
    string? CreatorName = null,
    int? ViewCount = null);