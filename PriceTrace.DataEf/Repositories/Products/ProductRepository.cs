using PriceTrace.Common.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;

namespace PriceTrace.DataEf.Repositories.Products;

public class ProductRepository : Repository<ProductEntity>, IProductRepository
{
    public ProductRepository(PriceTraceDbContext context) : base(context)
    {
    }
}
