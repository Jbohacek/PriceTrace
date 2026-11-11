using PriceTrace.DataEf.Repositories.Logs.Interfaces;
using PriceTrace.DataEf.Repositories.Products.Interfaces;
using PriceTrace.DataEf.Repositories.Users.Interfaces;

namespace PriceTrace.DataEf.UnitOfWork;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Products
    IProductRepository Products { get; }
    IListingRepository Listings { get; }
    IEshopPriceRepository EshopPrices { get; }

    // Users
    IUserRepository Users { get; }
    ISearchRequestsRepository SearchRequests { get; }

    // Logs
    ILogRepository Logs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    public Task SeedDatabase();
}
