# Postupy

Každý postup končí stejně: `dotnet build PriceTrace.slnx` musí projít bez chyb.

## Nový bazar (scraper)

1. Přidej hodnotu do `FleaMarketWebEnum` (`PriceTrace.Common/Enums/`). Čísla existujících
   hodnot neměň – jsou uložená v DB a chodí ve zprávách jako `int`.
2. V `PriceTrace.Application/Scrapers/` vytvoř třídu dědící z `BaseScraper`:
   - `BaseSearchUrl` se zástupnými znaky `{0}` (výraz), `{1}` (horní limit), `{2}` (dolní limit),
   - `GetSearchResult(IPage page, SearchRequestsEntity request)`; URL získáš z `PopulateUrl`.
3. Vrať implementaci `IScaperModelResult` (`PriceTrace.Contracts/ScapeModelResults/`). Můžeš
   založit vlastní třídu podle vzoru `BazosScraperResult`.
4. U každého `ListingEntity` vyplň aspoň `Title`, `Url` (absolutní, stabilní – slouží jako
   klíč deduplikace), `Price` a `Product`. Dodrž konvenci `-1` dohodou, `0` zdarma.
5. Doplň větev do `switch` v `ScrapingService.ExecuteSingleScrapingAsync`
   (soubor není v UTF-8, viz pravidlo 1 v `CLAUDE.md`).
6. Zpřístupni novou hodnotu v UI: výběr bazaru je v `SearchRequests.razor`.

Migrace není potřeba, enum je v DB jako `integer`.

## Nové pole na existující entitě

1. Vlastnost do entity v `PriceTrace.Common`.
2. Migrace:
   ```bash
   dotnet ef migrations add <Nazev> --project PriceTrace.DataEf --startup-project PriceTrace.ApiService
   ```
3. DTO v `PriceTrace.Contracts/Models/<X>Models/` – `Response` vždy, `Create`/`Update` podle
   potřeby. U pozičních recordů přidávej nové parametry na konec, ideálně s výchozí hodnotou.
4. Mapovací profil v `PriceTrace.ApiService/Mappings/` – rozšiř `ConstructUsing` pro Response.
5. Web: stránka v `PriceTrace.Web/Components/Pages/`, případně `productDetailCharts.js`.
6. Má-li pole plnit scraper, doplň ho v `BazosScraper` **a** v ručním kopírování
   `new ListingEntity { … }` v `ScrapingService` – jinak se do DB nedostane.
7. Má-li pole cestovat do scraperu, rozšiř `StartScrapingCommand`, `ScraperMappingProfile`,
   `ScrapingTask` a `StartScrapingCommandConsumer`.

## Nová entita

1. Třída v `PriceTrace.Common/<Oblast>/`, dědí z `BaseEntity`,
   `[Table("tbNazev", Schema = "<Schema>")]`.
2. `DbSet` a případné vztahy v `PriceTraceDbContext`.
3. `I<X>Repository : IRepository<XEntity>` a `XRepository : Repository<XEntity>` v
   `PriceTrace.DataEf/Repositories/<Oblast>/`.
4. Vlastnost v `IUnitOfWork` + líná inicializace v `UnitOfWork`; registrace v
   `DataEfExtensions.AddPriceTraceDataEf`.
5. Migrace.
6. Dál podle „Nový endpoint“.

## Nový endpoint / controller

1. DTO recordy do `PriceTrace.Contracts/Models/<X>Models/`, namespace
   `PriceTrace.Contracts.Models.<X>Models`.
2. Profil `<X>MappingProfile : Profile` v `PriceTrace.ApiService/Mappings/` (registruje se
   sám skenem assembly).
3. Controller v `PriceTrace.ApiService/Controllers/` s primárním konstruktorem
   `(IUnitOfWork unitOfWork, IMapper mapper)`. Vzor: `ProductsController`.
   - nenalezeno → `NotFound()`, vytvoření → `CreatedAtAction(nameof(GetById), …)`,
     smazání → `NoContent()`.
4. Přidej ukázkové požadavky do `PriceTrace.ApiService.http`.
5. Metodu do příslušného `*ApiClient` ve `PriceTrace.Web` (nový klient registruj v
   `Web/Program.cs` s `BaseAddress = new("https+http://apiservice")`).

## Nová stránka ve webu

1. `PriceTrace.Web/Components/Pages/<Nazev>.razor` s `@page`, `@rendermode InteractiveServer`
   a `@inject <X>ApiClient`.
2. Drž se vzoru existujících stránek: pole `isLoading`, `errorMessage`, načtení v
   `OnInitializedAsync`, `try/catch` kolem volání klienta.
3. Odkaz do `Components/Layout/NavMenu.razor`.
4. JS interop volej až v `OnAfterRenderAsync`; skripty přidávej do `Components/App.razor`.

## Ověření změny za běhu

1. Docker musí běžet. `dotnet run --project PriceTrace.AppHost`.
2. V Aspire dashboardu zkontroluj, že `apiservice`, `scraperservice` a `webfrontend` jsou
   ve stavu Running/Healthy. Logy scraperu jsou tamtéž v záložce Console.
3. API ověř přes Swagger UI (`/swagger` na `apiservice`), web přes `webfrontend`.
4. Scrapování: založ search request na seedovaný produkt „Steam deck“ a sleduj log
   `scraperservice` (`Scraping found N listings…`). Jde o skutečné požadavky na bazos.cz –
   nenastavuj krátké intervaly a po testu request smaž.
