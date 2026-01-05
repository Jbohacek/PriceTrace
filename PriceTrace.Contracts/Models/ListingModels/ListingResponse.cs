namespace PriceTrace.Contracts.Models.ListingModels;

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