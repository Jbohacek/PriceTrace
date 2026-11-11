using System.ComponentModel.DataAnnotations;

namespace PriceTrace.Contracts.Models.UserModels;

// User DTOs
public record CreateUserRequest(
    [property: Required(AllowEmptyStrings = false), MinLength(1)] string Username);