using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PriceTrace.Common.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;

namespace PriceTrace.DataEf.Repositories.Products;

public class ListingRepository : Repository<ListingEntity>, IListingRepository
{
    public ListingRepository(PriceTraceDbContext context) : base(context)
    {
    }

    public override IQueryable<ListingEntity> Query()
    {
        return DbSet.Include(l => l.Product);
    }

    public override async Task<ListingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<ListingEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .ToListAsync(cancellationToken);
    }

    public override async Task<IEnumerable<ListingEntity>> FindAsync(Expression<Func<ListingEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public override async Task<ListingEntity?> FirstOrDefaultAsync(Expression<Func<ListingEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }
}
