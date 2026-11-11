using System.ComponentModel.DataAnnotations;

namespace PriceTrace.Contracts.Models.ProductModels;

// Product DTOs
public record CreateProductRequest(
    [property: Required(AllowEmptyStrings = false)] string ProductName,
    string ProductDescription,
    [property: Required, MinLength(1, ErrorMessage = "At least one search name must be provided.")] string[] Names,
    string? ImageUrl = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ProductName))
            yield return new ValidationResult("ProductName cannot be empty.", [nameof(ProductName)]);

        if (Names is null || Names.Length == 0 || Names.All(string.IsNullOrWhiteSpace))
            yield return new ValidationResult("Names must contain at least one non-empty search term.", [nameof(Names)]);
    }
}