namespace PriceTrace.ApiService.Models;

// Product DTOs
public record CreateProductRequest(
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl = null);

public record UpdateProductRequest(
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl = null);

public record ProductResponse(
    Guid Id,
    string ProductName,
    string ProductDescription,
    string[] Names,
    string? ImageUrl,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
