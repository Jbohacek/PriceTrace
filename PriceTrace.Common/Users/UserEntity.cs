using PriceTrace.Common.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace PriceTrace.Common.Users;

[Table("tbUser",Schema = "User")]
public class UserEntity : BaseEntity
{
    public required string Username { get; set; }

    public List<SearchRequestsEntity> SearchRequests { get; set; } = new();
}