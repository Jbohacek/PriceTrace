using PriceTrace.Common.Users;

namespace PriceTrace.DataEf.Repositories.Users;

public class UserRepository : Repository<UserEntity>, IUserRepository
{
    public UserRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
