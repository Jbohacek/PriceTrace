namespace PriceTrace.ApiService.Models;

// User DTOs
public record CreateUserRequest(string Username);
public record UpdateUserRequest(string Username);
public record UserResponse(Guid Id, string Username, DateTime CreatedDate, DateTime? UpdatedDate);
