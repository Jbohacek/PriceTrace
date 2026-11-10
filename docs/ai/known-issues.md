# Pasti pro vývojáře

Věci, které v repu vypadají divně a na kterých se při práci snadno zasekneš: kódování,
názvy, zbytky šablony, nástroje. Slouží k tomu, abys je neobjevoval znovu a abys je omylem
„neopravil“ uprostřed jiného úkolu. Stav k 10. 11. 2026; když něco opravíš, položku smaž.

Skutečné chyby a nedostatky aplikace (scrapování, API, data, bezpečnost) jsou samostatně v
[issues.md](issues.md).

## Chování, které překvapí

- **`UpdatedDate = DateTime.Now` v controllerech je mrtvý kód** – `PriceTraceDbContext`
  hodnotu při ukládání přepíše na UTC.
- **`BaseEntity.CreatedDate` má výchozí `DateTime.Now`** (lokální čas). V DB skončí UTC díky
  `DbContext`, ale entita, která uložením neprošla, má lokální čas.
- **`SearchRequestsRepository.Query()` nenačítá navigace**, na rozdíl od jeho ostatních metod.
- **Mapování inzerátu bez `Include(Product)` spadne** na `NullReferenceException`
  (`ListingMappingProfile` čte `src.Product.Id`).
- **Opakovaný start scrapování se ignoruje.** `StartScrapingAsync` pro už běžící
  `SearchRequestId` jen zaloguje varování.
- **`StartScrapingCommand.ProductUrl` obsahuje `Product.ImageUrl`**
  (`ScraperMappingProfile`) a consumer ho nečte.
- **Odpověď `POST /api/listings/filter` je anonymní objekt**; web ji čte přes ručně udržovaný
  `FilteredListingsResponse` v `PriceTrace.Web/ListingApiClient.cs`.

## Názvy a struktura

Neměň bez výslovného zadání – přejmenování zasáhne hodně souborů.

- Složka a namespace `ScapeModelResults`, rozhraní `IScaperModelResult` (překlepy za
  „Scrape“/„Scraper“).
- `UpdateSearchRequestRequest` leží v `PriceTrace.Contracts`, ale má namespace
  `PriceTrace.ApiService.Models`. Proto mají `SearchRequestsController`,
  `SearchRequestMappingProfile` a `SearchRequestApiClient` `using PriceTrace.ApiService.Models;`.
- `LogRepository` je ve složce `LogsRepositories`, ale v namespace
  `PriceTrace.DataEf.Repositories.Logs`.
- Entita `SearchRequestsEntity` je v množném čísle, tabulka `tbSearchRequest` v jednotném.
- `LogLevelEnum.Informal` (míněno zřejmě „Information“).
- Uživatelská rozhraní repozitářů jsou v `Repositories/Users/` vedle implementací, produktová
  v podsložce `Interfaces/`.

## Zbytky šablony a mrtvý kód

- `PriceTrace.Web/WeatherApiClient.cs` + jeho registrace ve `Web/Program.cs`; endpoint
  `/weatherforecast` v API neexistuje (první požadavek v `.http` souboru taky ne).
- `PriceTrace.Web/Components/Pages/Counter.razor`.
- `PriceTrace.Web/wwwroot/lib/bootstrap/` – nepoužívá se, Bootstrap jde z CDN.
- `PriceTrace.Clients` – prázdný projekt, ale drží tranzitivní reference (viz architecture.md).
- `EshopPriceEntity`, `LogEntity` a jejich repozitáře; transakční metody a `SeedDatabase()`
  na `IUnitOfWork`; `IScrapingService.IsRunning`.
- `RabbitMQ.Client` v `ApiService` a `Aspire.RabbitMQ.Client` ve `ScraperService` – připojení
  řeší MassTransit sám.
- Prázdná položka `<Folder Include="Models\" />` ve `ScraperService.csproj`.

## Prostředí a nástroje

- **Pět souborů ve Windows-1250** – seznam a postup je v `CLAUDE.md`, pravidlo 1.
- **Konce řádků.** `.editorconfig` požaduje CRLF, `.gitattributes` má `* text=auto`. Na Linuxu
  jsou v pracovní kopii LF; nepřeváděj je hromadně.
- **`README.md` uvádí pro instalaci Playwrightu složku projektu `Web`**, kde skript není –
  viz [scraping.md](scraping.md#instalace-prohlížečů).
- **Kořenový `playwright.ps1`** hledá `Microsoft.Playwright.dll` vedle sebe, takže z kořene
  repa nefunguje; je to kopie skriptu z výstupní složky buildu.
- **Aspire verze.** `Aspire.AppHost.Sdk` je 9.5.0, hostingové balíčky 13.1.0.
- **Pevný port 5432** pro Postgres koliduje s lokálně běžící instancí.
- **Testy** startují celý AppHost s 30s timeouty; bez Dockeru nebo při studeném stahování
  obrazů selžou.
