using PriceTrace.Common.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;

namespace PriceTrace.DataEf.Repositories.Products;

public class ListingRepository : Repository<ListingEntity>, IListingRepository
{
    public ListingRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
