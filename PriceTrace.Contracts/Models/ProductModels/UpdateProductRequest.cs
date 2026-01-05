namespace PriceTrace.Contracts.Models.ProductModels;

public record UpdateProductRequest(
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl = null);