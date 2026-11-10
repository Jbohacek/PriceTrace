# PriceTrace – průvodce pro AI agenty

PriceTrace je proof of concept „chytrého rádce pro bazary“: uživatel si založí produkt a
hlídací požadavek (search request), služba pak v intervalu prochází Bazoš.cz a ukládá nalezené
inzeráty. Web nad nimi ukazuje seznamy, filtry a grafy cen.

Tento soubor je vstupní bod. Podrobnosti jsou v `docs/ai/`:

| Soubor | Kdy ho číst |
| --- | --- |
| [docs/ai/architecture.md](docs/ai/architecture.md) | Než sáhneš na víc než jeden projekt, na DI nebo na Aspire |
| [docs/ai/scraping.md](docs/ai/scraping.md) | Cokoli kolem scraperu, MassTransit zpráv a Playwrightu |
| [docs/ai/data-and-api.md](docs/ai/data-and-api.md) | Entity, migrace, REST endpointy, DTO, Blazor stránky |
| [docs/ai/recipes.md](docs/ai/recipes.md) | Postupy: nový scraper, nová entita, nový endpoint, nová stránka |
| [docs/ai/issues.md](docs/ai/issues.md) | Chyby a nedostatky aplikace podle závažnosti, s místem v kódu a návrhem opravy |
| [docs/ai/known-issues.md](docs/ai/known-issues.md) | Pasti pro vývojáře – přečti, než začneš „opravovat“ něco, co vypadá divně |

## Stack

- .NET 10 (`net10.0`), C# s `Nullable` a `ImplicitUsings`, řešení ve formátu `PriceTrace.slnx`
- .NET Aspire (AppHost) – orchestruje PostgreSQL, RabbitMQ a tři služby
- ASP.NET Core Web API + AutoMapper 12 + Swashbuckle
- EF Core 10 + Npgsql, vzor Repository + Unit of Work
- MassTransit 8.3 nad RabbitMQ (API → scraper)
- Microsoft.Playwright 1.57 (headless Chromium)
- Blazor Web App, Interactive Server; Bootstrap 5, Bootstrap Icons a Chart.js z CDN
- xUnit v3 + `Aspire.Hosting.Testing`

Verze balíčků jsou přímo v jednotlivých `.csproj`. `Directory.Packages.props` existuje, ale
centrální správa verzí je v něm vypnutá.

## Projekty

| Projekt | Role |
| --- | --- |
| `PriceTrace.AppHost` | Aspire orchestrace, jediný správný způsob spuštění celku |
| `PriceTrace.ServiceDefaults` | OpenTelemetry, health checky, service discovery (používá API a Web) |
| `PriceTrace.ApiService` | REST API, AutoMapper profily, publikuje příkazy pro scraper, spouští migrace |
| `PriceTrace.ScraperService` | Worker: MassTransit consumery, drží běžící scrapovací smyčky |
| `PriceTrace.Web` | Blazor frontend, s API mluví přes typované `HttpClient` třídy |
| `PriceTrace.Application` | Logika scrapování: `ScrapingService`, `PlaywrightService`, `BaseScraper`, `BazosScraper` |
| `PriceTrace.DataEf` | `PriceTraceDbContext`, migrace, repozitáře, `UnitOfWork` |
| `PriceTrace.Contracts` | DTO (request/response), MassTransit zprávy, `ScrapingTask`, výsledky scraperů |
| `PriceTrace.Common` | Entity, enumy, `BaseEntity` – nezávisí na ničem |
| `PriceTrace.Clients` | Prázdný projekt (jen `.csproj`), slouží jen jako mezičlánek referencí |
| `PriceTrace.Tests` | Jeden integrační smoke test přes Aspire |

## Příkazy

```bash
# build celého řešení
dotnet build PriceTrace.slnx

# spuštění všeho (vyžaduje běžící Docker)
dotnet run --project PriceTrace.AppHost

# testy (vyžadují Docker – startují celý AppHost)
dotnet test PriceTrace.Tests

# nová migrace
dotnet ef migrations add <Nazev> --project PriceTrace.DataEf --startup-project PriceTrace.ApiService
```

Playwright potřebuje jednorázově stáhnout prohlížeče, viz
[docs/ai/scraping.md](docs/ai/scraping.md#instalace-prohlížečů).

Po startu AppHostu: Aspire dashboard na `https://localhost:17205`, Swagger UI na
`/swagger` služby `apiservice` (port přiděluje Aspire, najdeš ho v dashboardu). Postgres
běží na pevném portu 5432 a kontejnery jsou perzistentní – přežijí zastavení AppHostu.

## Pravidla, která musíš dodržet

1. **Kódování souborů.** Pět souborů není v UTF-8 (Windows-1250, kvůli českým komentářům):
   `PriceTrace.ApiService/Program.cs`, `PriceTrace.Application/Services/ScrapingService.cs`,
   `PriceTrace.ScraperService/Consumers/StartScrapingCommandConsumer.cs`,
   `PriceTrace.Tests/WebTests.cs`, `PriceTrace.Web/Components/Pages/Listings.razor`.
   Nástroje Edit/Write je odmítnou nebo rozbijí diakritiku. Před úpravou se zeptej uživatele,
   jestli je smíš převést do UTF-8; jinak je upravuj shellem se zachováním kódování.
   `grep` je bere jako binární – použij `grep -a`. Nové soubory piš v UTF-8.
2. **Směr závislostí.** `Common` ← `Contracts`/`DataEf` ← `Clients` ← `Application` ← služby.
   `Web` smí referencovat jen `Contracts` a `ServiceDefaults` – nikdy `DataEf` ani entity přímo
   do UI logiky. Entity patří do `Common`, DTO a zprávy do `Contracts`.
3. **Přístup k datům jen přes `IUnitOfWork`.** Controllery ani služby si neinjektují
   `PriceTraceDbContext`. Pro `Include` použij `unitOfWork.<Repo>.Query()`.
4. **Časy v UTC.** Sloupce jsou `timestamp with time zone`; Npgsql odmítne `DateTime` s
   `Kind=Local`. `CreatedDate`/`UpdatedDate` přepisuje `DbContext` při ukládání sám, v novém
   kódu používej `DateTime.UtcNow`.
5. **Změna modelu = migrace.** Po úpravě entity vždy přidej migraci a commitni ji včetně
   `PriceTraceDbContextModelSnapshot.cs`. Migrace se aplikují samy při startu `ApiService`
   v prostředí Development.
6. **Mapování přes AutoMapper profily** v `PriceTrace.ApiService/Mappings/`. Odpovědi jsou
   poziční `record` typy, proto profily používají `ConstructUsing`.
7. **Zprávy MassTransit jsou kontrakt mezi dvěma procesy.** Změna `StartScrapingCommand` nebo
   `StopScrapingCommand` vyžaduje úpravu publisheru (API) i consumeru (scraper) zároveň.
8. **Neuklízej mimo zadání.** V repu jsou překlepy v názvech (`ScapeModelResults`,
   `IScaperModelResult`), špatné namespace a zbytky šablony. Jsou sepsané v
   [known-issues.md](docs/ai/known-issues.md); přejmenovávej je jen na výslovnou žádost.

## Konvence kódu

- File-scoped namespace, odsazení 4 mezery (`.editorconfig`).
- Novější třídy používají primární konstruktory (`class X(IDep dep)`); drž se stylu souboru,
  který upravuješ.
- Entity: přípona `Entity`, dědí z `BaseEntity`, atribut `[Table("tbNazev", Schema = "...")]`.
- DTO: `Create<X>Request`, `Update<X>Request`, `<X>Response` jako `record` v
  `PriceTrace.Contracts/Models/<X>Models/`.
- Controllery: `[Route("api/[controller]")]`, id v cestě jako `{id:guid}`.
- Identifikátory, logy a texty v UI anglicky; komentáře v kódu bývají česky.
- Commit zprávy v repu jsou krátké a česky.

## Stav projektu

Proof of concept. Není tu autentizace ani autorizace, uživatel se v UI vybírá ze seznamu.
Testy prakticky neexistují. Podporovaný je jediný bazar (Bazoš). Entity `EshopPriceEntity`
a `LogEntity` mají tabulky i repozitáře, ale nic je nepoužívá.
