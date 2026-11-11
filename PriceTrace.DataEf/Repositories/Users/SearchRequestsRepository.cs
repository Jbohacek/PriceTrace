using Microsoft.EntityFrameworkCore;
using PriceTrace.Common.Users;
using PriceTrace.DataEf.Repositories.Users.Interfaces;
using System.Linq.Expressions;

namespace PriceTrace.DataEf.Repositories.Users;

public class SearchRequestsRepository : Repository<SearchRequestsEntity>, ISearchRequestsRepository
{
    public SearchRequestsRepository(PriceTraceDbContext context) : base(context)
    {
    }

    public override IQueryable<SearchRequestsEntity> Query()
    {
        return DbSet
            .Include(sr => sr.User)
            .Include(sr => sr.Product);
    }

    public override async Task<SearchRequestsEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<SearchRequestsEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .ToListAsync(cancellationToken);
    }

    public override async Task<IEnumerable<SearchRequestsEntity>> FindAsync(Expression<Func<SearchRequestsEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public override async Task<SearchRequestsEntity?> FirstOrDefaultAsync(Expression<Func<SearchRequestsEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }
}
