using PriceTrace.Common.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;

namespace PriceTrace.DataEf.Repositories.Products;

public class EshopPriceRepository : Repository<EshopPriceEntity>, IEshopPriceRepository
{
    public EshopPriceRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
