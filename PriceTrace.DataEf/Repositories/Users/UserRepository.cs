using PriceTrace.Common.Users;
using PriceTrace.DataEf.Repositories.Users.Interfaces;

namespace PriceTrace.DataEf.Repositories.Users;

public class UserRepository : Repository<UserEntity>, IUserRepository
{
    public UserRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
