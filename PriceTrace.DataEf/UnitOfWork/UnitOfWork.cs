using Microsoft.EntityFrameworkCore.Storage;
using PriceTrace.DataEf.Repositories.Logs;
using PriceTrace.DataEf.Repositories.Logs.Interfaces;
using PriceTrace.DataEf.Repositories.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;
using PriceTrace.DataEf.Repositories.Users;
using PriceTrace.DataEf.Repositories.Users.Interfaces;

namespace PriceTrace.DataEf.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly PriceTraceDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProductRepository? _products;
    private IListingRepository? _listings;
    private IEshopPriceRepository? _eshopPrices;
    private IUserRepository? _users;
    private ISearchRequestsRepository? _searchRequests;
    private ILogRepository? _logs;

    public UnitOfWork(PriceTraceDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IListingRepository Listings => _listings ??= new ListingRepository(_context);
    public IEshopPriceRepository EshopPrices => _eshopPrices ??= new EshopPriceRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ISearchRequestsRepository SearchRequests => _searchRequests ??= new SearchRequestsRepository(_context);
    public ILogRepository Logs => _logs ??= new LogRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task SeedDatabase()
    {
        await PriceTraceDbContext.SeedDataAsync(_context);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
