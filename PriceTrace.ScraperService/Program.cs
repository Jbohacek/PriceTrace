using PriceTrace.ScraperService;
using PriceTrace.ScraperService.Consumers;
using PriceTrace.Application.Services;
using PriceTrace.DataEf.Extensions;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

// Add PriceTrace database with UnitOfWork + Repository pattern
builder.AddPriceTraceDataEf();

// Register PlaywrightService as singleton
builder.Services.AddSingleton<IPlaywrightService, PlaywrightService>();

// Register ScrapingService as singleton (uses IServiceScopeFactory for scoped dependencies)
builder.Services.AddSingleton<IScrapingService, ScrapingService>();

// Add Worker service
builder.Services.AddHostedService<Worker>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<StartScrapingCommandConsumer>();
    x.AddConsumer<StopScrapingCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Get RabbitMQ connection from Aspire configuration
        var rabbitMqConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
        
        if (!string.IsNullOrEmpty(rabbitMqConnectionString))
        {
            cfg.Host(new Uri(rabbitMqConnectionString));
        }
        else
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        }

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

// Initialize PlaywrightService on startup
var playwrightService = host.Services.GetRequiredService<IPlaywrightService>();
await playwrightService.InitializeAsync();

host.Run();
