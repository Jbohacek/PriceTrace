using PriceTrace.ScraperService;
using PriceTrace.ScraperService.Consumers;
using PriceTrace.Application.Services;
using PriceTrace.DataEf.Extensions;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add PriceTrace database with UnitOfWork + Repository pattern
builder.AddPriceTraceDataEf();

// Register PlaywrightService as singleton
builder.Services.AddSingleton<IPlaywrightService, PlaywrightService>();

// Register ScrapingService as singleton (uses IServiceScopeFactory for scoped dependencies)
builder.Services.AddSingleton<IScrapingService, ScrapingService>();

// Resume existing search requests from DB on startup
builder.Services.AddHostedService<ScrapingStartupService>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StartScrapingCommandConsumer>();
    x.AddConsumer<StopScrapingCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
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

        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

// Initialize PlaywrightService on startup
var playwrightService = host.Services.GetRequiredService<IPlaywrightService>();
await playwrightService.InitializeAsync();

host.Run();
