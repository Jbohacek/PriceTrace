namespace PriceTrace.Contracts.Models.ProductModels;

// Product DTOs
public record CreateProductRequest(
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl = null);