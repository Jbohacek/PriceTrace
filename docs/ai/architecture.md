# Architektura

## Běhová topologie

Vše skládá `PriceTrace.AppHost/AppHost.cs`:

```
                    ┌──────────────┐  HTTP (service discovery "apiservice")
   prohlížeč ─────► │ webfrontend  │ ───────────────────────────┐
                    │ Blazor Server│                            ▼
                    └──────────────┘                   ┌────────────────┐
                                                       │   apiservice   │
                                                       │  REST + Swagger│
                                                       └───┬────────┬───┘
                                          EF Core / Npgsql │        │ MassTransit Publish
                                                           ▼        ▼
                                                   ┌──────────┐  ┌──────────┐
                                                   │ postgres │  │ rabbitmq │
                                                   │ db:      │  └────┬─────┘
                                                   │pricetrace│       │ Consume
                                                   └──────────┘       ▼
                                                         ▲     ┌────────────────┐
                                                         └─────│ scraperservice │──► bazos.cz
                                                      EF Core  │ Worker+Chromium│   (Playwright)
                                                               └────────────────┘
```

| Aspire resource | Co to je | Poznámky |
| --- | --- | --- |
| `postgres` | PostgreSQL kontejner + pgAdmin | pevný host port 5432, `ContainerLifetime.Persistent`, heslo z parametru `postgres-password` (`PriceTrace.AppHost/appsettings.json`) |
| `pricetrace` | databáze na `postgres` | název connection stringu, který čeká `AddPriceTraceDataEf()` |
| `rabbitmq` | RabbitMQ s management pluginem | perzistentní kontejner |
| `apiservice` | `PriceTrace.ApiService` | health check `/health`, čeká na DB a RabbitMQ |
| `scraperservice` | `PriceTrace.ScraperService` | čeká na DB, RabbitMQ i `apiservice` (kvůli migracím před startem `ScrapingStartupService`) |
| `webfrontend` | `PriceTrace.Web` | externí HTTP endpoint, čeká na API i scraper |

Perzistentní kontejnery znamenají, že data i fronty přežijí restart AppHostu. Když potřebuješ
čistou databázi, musíš kontejner (a jeho volume) smazat v Dockeru ručně.

## Závislosti mezi projekty

```
Common
  ├── Contracts ──────────────┬──────► ApiService (+ DataEf, ServiceDefaults)
  └── DataEf ─────────────────┤
                              ▼
                         Application
                              ▲
                       ScraperService (+ Contracts, DataEf, ServiceDefaults)

Web ──► Contracts, ServiceDefaults
AppHost ──► ApiService, ScraperService, Web
Tests ──► AppHost, Application, Contracts
```

Důsledky, které stojí za to znát:

- `Contracts` referencuje `Common`, takže DTO mohou používat enumy (`FleaMarketWebEnum`) a
  `IScraperModelResult` dokonce vrací `ListingEntity`. Přes `Contracts` se tak entity dostanou
  i do `Web` – nepoužívej je tam.
- `ScraperService` referencuje `ServiceDefaults`, takže má zapnutou OpenTelemetry telemetrii a strukturované logování v Aspire dashboardu.

## Registrace služeb (DI)

| Kde | Co | Životnost |
| --- | --- | --- |
| `DataEfExtensions.AddPriceTraceDataEf()` | `PriceTraceDbContext` (přes `AddNpgsqlDbContext`), `IUnitOfWork`, všechny `I*Repository` | scoped |
| `ApiService/Program.cs` | AutoMapper (skenuje assembly API), MassTransit bez consumerů, `IScraperCommandService` | scoped |
| `ScraperService/Program.cs` | `IPlaywrightService`, `IScrapingService` | **singleton** |
| `ScraperService/Program.cs` | `ScrapingStartupService` (`IHostedService` – obnova úloh z DB při startu) | singleton (hosted service) |
| `ScraperService/Program.cs` | MassTransit + `StartScrapingCommandConsumer`, `StopScrapingCommandConsumer` (s `UseMessageRetry`) | scoped (per zpráva) |
| `Web/Program.cs` | typované klienty `ProductApiClient`, `ListingApiClient`, `SearchRequestApiClient`, `UserApiClient` s adresou `https+http://apiservice` | transient |

`ScrapingService` je singleton, ale potřebuje scoped `IUnitOfWork`. Proto si pro každý běh
vytváří vlastní scope přes `IServiceScopeFactory`. Nikdy do něj neinjektuj `IUnitOfWork` ani
`DbContext` konstruktorem.

`UnitOfWork` si repozitáře vytváří líně sám (`new ProductRepository(_context)`); samostatné
registrace `I*Repository` v DI existují také, ale kód je nepoužívá.

## Konfigurace

- **Connection stringy** dodává Aspire jako `ConnectionStrings:pricetrace` a
  `ConnectionStrings:rabbitmq`. Hodnota v `appsettings.json` (`Host=localhost;Database=postgres;…`)
  je jen záloha pro `dotnet ef` a běh mimo Aspire.
- **RabbitMQ**: oba procesy čtou `ConnectionStrings:rabbitmq`; když chybí, padají zpět na
  `localhost` s `guest/guest`. Tento blok je v obou `Program.cs` zkopírovaný – měň oba.
- **Migrace a seed** (`app.MigrateDatabaseAsync()`) běží v `ApiService` při každém startu bez ohledu na prostředí. Seed vloží produkt „Steam deck“ a uživatele „Admin“, pokud jsou tabulky prázdné.
- **Health endpointy** `/health` a `/alive` se mapují ve všech prostředích (`ServiceDefaults/Extensions.cs`), protože na nich závisí orchestrace v `AppHost.cs`.
- Tajemství nejsou řešena: heslo k Postgresu je `postgres` v commitnutém `appsettings.json`.

## Testy

- `PriceTrace.Tests/WebTests.cs` obsahuje integrační test: nastartuje celý AppHost přes
  `DistributedApplicationTestingBuilder`, počká, až bude `webfrontend` zdravý, a ověří, že `/`
  vrátí 200. Vyžaduje Docker a trvá dlouho.
- `PriceTrace.Tests/ScraperAndValidationTests.cs` obsahuje jednotkové testy pro parsování cen (`BazosScraper.ParsePrice`), generování URL (`BaseScraper.PopulateUrl`) a validaci vstupních DTO.
- Drž se xUnit v3 (`TestContext.Current.CancellationToken`).
