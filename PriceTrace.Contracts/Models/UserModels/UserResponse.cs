namespace PriceTrace.Contracts.Models.UserModels;

public record UserResponse(Guid Id, string Username, DateTime CreatedDate, DateTime? UpdatedDate);