using System.ComponentModel.DataAnnotations;
using PriceTrace.Common.Enums;

namespace PriceTrace.Contracts.Models.SearchRequestModels;

// SearchRequest DTOs
public record CreateSearchRequestRequest(
    Guid UserId,
    Guid ProductId,
    FleaMarketWebEnum FleaMarketWeb,
    [property: Range(0, int.MaxValue)] int? SoftUpLimit = null,
    [property: Range(0, int.MaxValue)] int? SoftDownLimit = null,
    [property: Range(0, int.MaxValue)] int? HardUpLimit = null,
    [property: Range(0, int.MaxValue)] int? HardDownLimit = null,
    [property: Range(60, long.MaxValue, ErrorMessage = "IntervalInSeconds must be at least 60 seconds.")] long IntervalInSeconds = 300) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserId == Guid.Empty)
            yield return new ValidationResult("UserId is required.", [nameof(UserId)]);

        if (ProductId == Guid.Empty)
            yield return new ValidationResult("ProductId is required.", [nameof(ProductId)]);

        if (FleaMarketWeb == FleaMarketWebEnum.unknown || !Enum.IsDefined(FleaMarketWeb))
            yield return new ValidationResult("A valid FleaMarketWeb marketplace must be selected.", [nameof(FleaMarketWeb)]);

        if (HardDownLimit.HasValue && HardUpLimit.HasValue && HardDownLimit.Value > HardUpLimit.Value)
            yield return new ValidationResult("HardDownLimit cannot be greater than HardUpLimit.", [nameof(HardDownLimit), nameof(HardUpLimit)]);

        if (SoftDownLimit.HasValue && SoftUpLimit.HasValue && SoftDownLimit.Value > SoftUpLimit.Value)
            yield return new ValidationResult("SoftDownLimit cannot be greater than SoftUpLimit.", [nameof(SoftDownLimit), nameof(SoftUpLimit)]);
    }
}