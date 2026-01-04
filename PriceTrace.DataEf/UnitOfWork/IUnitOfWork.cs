using PriceTrace.DataEf.Repositories.LogsRepositories.Interfaces;
using PriceTrace.DataEf.Repositories.Products.Interfaces;
using PriceTrace.DataEf.Repositories.Users;

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
