using System.ComponentModel.DataAnnotations;

namespace PriceTrace.Contracts.Models.UserModels;

public record UpdateUserRequest(
    [property: Required(AllowEmptyStrings = false), MinLength(1)] string Username);