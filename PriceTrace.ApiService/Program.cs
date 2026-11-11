using MassTransit;
using PriceTrace.ApiService.Mappings;
using PriceTrace.ApiService.Services;
using PriceTrace.DataEf.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PriceTrace database with UnitOfWork + Repository pattern
builder.AddPriceTraceDataEf();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
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

// Register ScraperCommandService
builder.Services.AddScoped<IScraperCommandService, ScraperCommandService>();

// Add controllers
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI();

    // Aplikuj migrace v development módu
    await app.MigrateDatabaseAsync();
}

// Map controllers
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();