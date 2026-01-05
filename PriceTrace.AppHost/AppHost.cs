using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithHostPort(5432)
    .WithPgAdmin();

var databaseName = "pricetrace";
var creationScript = $$"""
                       -- Create the database
                       CREATE DATABASE {{databaseName}};

                       """;

var database = postgres.AddDatabase("pricetrace").WithCreationScript(creationScript);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var scraperService = builder.AddProject<Projects.PriceTrace_ScraperService>("scraperservice")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WaitFor(postgres)
    .WithReference(database);

var apiService = builder.AddProject<PriceTrace_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WaitFor(postgres)
    .WithReference(database)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.PriceTrace_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WaitFor(scraperService);

builder.Build().Run();