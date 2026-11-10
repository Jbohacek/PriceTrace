# Známé problémy a pasti

Seznam věcí, které v kódu vypadají jako chyba nebo jí opravdu jsou. Slouží k tomu, abys je
neobjevoval znovu a abys je omylem „neopravil“ uprostřed jiného úkolu. Stav k 10. 11. 2026;
když něco opravíš, položku smaž.

Označení: **[ověřeno]** plyne přímo z kódu, **[podezření]** nebylo ověřeno za běhu.

## Scrapování

- **[ověřeno] Běžící úlohy jsou jen v paměti.** `ScrapingService._runningTasks` se po restartu
  `scraperservice` vyprázdní a nic ho znovu nenaplní. Existující search requesty se rozběhnou
  až po ručním `POST /api/scraper/start/{id}`.
- **[ověřeno] `PUT /api/searchrequests/{id}` nerestartuje úlohu.** Běžící smyčka dál používá
  staré limity i interval.
- **[ověřeno] Opakovaný start se ignoruje.** `StartScrapingAsync` pro už běžící
  `SearchRequestId` jen zaloguje varování.
- **[ověřeno] Jedna sdílená `IPage`.** `PlaywrightService` vytváří jedinou stránku a
  `ScrapingService` ji půjčuje všem smyčkám bez zámku. Dva souběžně běžící requesty si
  navzájem přepíšou navigaci a mohou uložit inzeráty k nesprávnému produktu.
- **[ověřeno] Deduplikace podle `Url` je globální.** Inzerát, který odpovídá dvěma produktům,
  se uloží jen k tomu, jehož smyčka ho našla první.
- **[ověřeno] Existující inzeráty se neaktualizují.** Změna ceny u stejné URL se nezaznamená,
  takže graf „Price Over Time“ ve skutečnosti ukazuje ceny různých inzerátů podle data
  nalezení, ne vývoj ceny jednoho inzerátu.
- **[ověřeno] `SoftUpLimit`/`SoftDownLimit` se nikde nevyhodnocují.**
- **[ověřeno] `StartScrapingCommand.ProductUrl` obsahuje `Product.ImageUrl`**
  (`ScraperMappingProfile`) a consumer ho nečte.
- **[ověřeno] `BazosScraper` se vytváří bez loggeru** (`new BazosScraper()`), takže jeho
  průběžné logy se nikam nepíšou.
- **[ověřeno] Cena „Zdarma“ i „Dohodou“ končí jako `-1`**, protože cokoli, co neprojde
  `double.TryParse`, dostane `-1`. Hodnota `0` (zdarma) se ze scraperu nikdy nevrátí.
- **[podezření] Parsování ceny závisí na kultuře procesu.** `double.TryParse(price, out …)`
  bez `CultureInfo` může podle prostředí selhat na oddělovači tisíců („5 500“) a cenu
  označit jako `-1`.
- **[ověřeno] Dotaz do DB pro každý inzerát.** `ExecuteSingleScrapingAsync` volá
  `FirstOrDefaultAsync(l => l.Url == …)` v cyklu; nad `Url` není index.

## API a data

- **[ověřeno] `ProductDetail.razor` žádá `PageSize: 1000`, API ořezává na 100.** Statistiky a
  grafy na detailu produktu se počítají nejvýš ze 100 inzerátů.
- **[ověřeno] `UpdatedDate = DateTime.Now` v controllerech je mrtvý kód** – `DbContext` hodnotu
  při ukládání přepíše na UTC.
- **[ověřeno] `BaseEntity.CreatedDate` má výchozí `DateTime.Now`** (lokální čas). V DB skončí
  UTC díky `DbContext`, ale entita, která uložením neprošla, má lokální čas.
- **[ověřeno] `DELETE /api/searchrequests/{id}` publikuje stop až po smazání.** Když
  publikování selže, řádek je pryč a smyčka běží dál, dokud se scraper nerestartuje.
- **[ověřeno] Smazání produktu nezastaví jeho smyčky.** Kaskáda smaže search requesty v DB,
  ale `StopScrapingCommand` se nepošle; smyčka pak jen loguje „Product … not found“.
- **[ověřeno] Žádná autentizace, autorizace ani validace vstupů.** Mimo jiné
  `IntervalInSeconds` nemá dolní mez.
- **[ověřeno] `SearchRequestsRepository.Query()` nenačítá navigace**, na rozdíl od jeho
  ostatních metod.
- **[ověřeno] Heslo k Postgresu je v repu** (`PriceTrace.AppHost/appsettings.json`,
  `postgres`).

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
  na `IUnitOfWork`.
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
