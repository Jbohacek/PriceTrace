using Microsoft.EntityFrameworkCore;
using PriceTrace.Common.LogsEntities;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;

namespace PriceTrace.DataEf;

public class PriceTraceDbContext : DbContext
{
    public PriceTraceDbContext(DbContextOptions<PriceTraceDbContext> options) : base(options)
    {
    }

    // Products schema
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ListingEntity> Listings => Set<ListingEntity>();
    public DbSet<EshopPriceEntity> EshopPrices => Set<EshopPriceEntity>();

    // User schema
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SearchRequestsEntity> SearchRequests => Set<SearchRequestsEntity>();

    // Logs
    public DbSet<LogEntity> Logs => Set<LogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Products schema
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Listings)
                  .WithOne(l => l.Product)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.EshopPrices)
                  .WithOne(ep => ep.Product)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.SearchRequests)
                  .WithOne(sr => sr.Product)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // User schema
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasMany(e => e.SearchRequests)
                  .WithOne(sr => sr.User)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Common.Core.IBaseEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Common.Core.IBaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedDate = DateTime.UtcNow;
            }
            else
            {
                entity.UpdatedDate = DateTime.UtcNow;
            }
        }
    }
}
