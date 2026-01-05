namespace PriceTrace.Contracts.Models.ProductModels;

public record ProductResponse(
    Guid Id,
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl,
    DateTime CreatedDate,
    DateTime? UpdatedDate);