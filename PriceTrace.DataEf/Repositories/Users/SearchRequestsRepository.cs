using PriceTrace.Common.Users;

namespace PriceTrace.DataEf.Repositories.Users;

public class SearchRequestsRepository : Repository<SearchRequestsEntity>, ISearchRequestsRepository
{
    public SearchRequestsRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
