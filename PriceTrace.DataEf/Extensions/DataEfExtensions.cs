using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceTrace.DataEf.Repositories;
using PriceTrace.DataEf.Repositories.Logs;
using PriceTrace.DataEf.Repositories.Logs.Interfaces;
using PriceTrace.DataEf.Repositories.Products;
using PriceTrace.DataEf.Repositories.Products.Interfaces;
using PriceTrace.DataEf.Repositories.Users;
using PriceTrace.DataEf.Repositories.Users.Interfaces;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.DataEf.Extensions;

public static class DataEfExtensions
{
    public static IHostApplicationBuilder AddPriceTraceDataEf(this IHostApplicationBuilder builder, string connectionName = "pricetrace")
    {
        builder.AddNpgsqlDbContext<PriceTraceDbContext>(connectionName);

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IListingRepository, ListingRepository>();
        builder.Services.AddScoped<IEshopPriceRepository, EshopPriceRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISearchRequestsRepository, SearchRequestsRepository>();
        builder.Services.AddScoped<ILogRepository, LogRepository>();

        return builder;
    }

    public static async Task MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PriceTraceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PriceTraceDbContext>>();

        try
        {
            logger.LogInformation("Starting database migration...");

            await dbContext.Database.MigrateAsync();

            await PriceTraceDbContext.SeedDataAsync(dbContext);

            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}
