using PriceTrace.ScraperService;
using PriceTrace.ScraperService.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
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
host.Run();
