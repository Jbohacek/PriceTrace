using PriceTrace.Common.Core;

namespace PriceTrace.Common.Users;

public class UserEntity : BaseEntity
{
    public required string Username { get; set; }

    public List<SearchRequestsEntity> SearchRequests { get; set; } = new();
}