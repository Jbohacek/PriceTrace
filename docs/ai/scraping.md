# Scrapování

## Tok od kliknutí po uložený inzerát

1. **Web** – stránka `SearchRequests.razor` zavolá `POST /api/searchrequests`.
2. **API** – `SearchRequestsController.Create` uloží `SearchRequestsEntity`, namapuje ji na
   `StartScrapingCommand` (`ScraperMappingProfile`) a přes `IScraperCommandService` zavolá
   `IPublishEndpoint.Publish`.
3. **RabbitMQ** – MassTransit doručí zprávu do fronty consumeru (názvy front generuje
   `cfg.ConfigureEndpoints(context)`).
4. **Scraper** – `StartScrapingCommandConsumer` si request znovu načte z DB (kvůli
   `IntervalInSeconds` a `Product.Names`, které ve zprávě nejsou), sestaví `ScrapingTask` a
   předá ho `IScrapingService.StartScrapingAsync`.
5. **Smyčka** – `ScrapingService` spustí `Task.Run` s nekonečnou smyčkou
   `ExecuteScrapingLoopAsync` a uloží úlohu do `ConcurrentDictionary<Guid, ScrapingTask>`
   (klíč = `SearchRequestId`). Při startu `ScraperService` navíc `ScrapingStartupService` načte všechny
   existující `SearchRequestsEntity` z DB a jejich smyčky automaticky obnoví. Každá iterace:
   - vytvoří DI scope a vezme `IUnitOfWork`,
   - znovu načte `SearchRequestsEntity` z DB (pokud mezitím zanikla, smyčka se ukončí; jinak si zaktualizuje interval, limity a názvy),
   - vyžádá si novou izolovanou `IPage` z `IPlaywrightService.GetPageAsync()` (a po skončení ji v `finally` zavře),
   - vybere scraper podle `FleaMarketWeb` (`switch` v `ExecuteSingleScrapingAsync`),
   - zavolá `scraper.GetSearchResult(page, searchRequest)`,
   - jedním dávkovým dotazem načte existující inzeráty pro daný `(ProductId, Url)`; nové vloží (a vyhodnotí shodu se soft limity), u existujících aktualizuje změněnou cenu/popis/titulek a `RetrievedAt`,
   - uloží a počká `IntervalInSeconds` (výchozí 300 s). Po chybě čeká 60 s a zkouší znovu.
6. **Zastavení a změny** – `DELETE /api/searchrequests/{id}`, smazání produktu/uživatele nebo `POST /api/scraper/stop/{id}`
   publikuje `StopScrapingCommand`; consumer zavolá `StopScrapingAsync`, který zruší
   `CancellationTokenSource` a úlohu odebere ze slovníku. `PUT /api/searchrequests/{id}` publikuje `StopScrapingCommand` následovaný `StartScrapingCommand`.

Ruční ovládání: `POST /api/scraper/start/{searchRequestId}` a
`POST /api/scraper/stop/{searchRequestId}` (`ScraperController`). Web pro ně tlačítka nemá,
volají se přes Swagger nebo `PriceTrace.ApiService/PriceTrace.ApiService.http`.

## Klíčové typy

| Typ | Umístění | Role |
| --- | --- | --- |
| `StartScrapingCommand`, `StopScrapingCommand` | `PriceTrace.Contracts/Messages/` | zprávy na sběrnici (poziční `record`) |
| `ScrapingTask` | `PriceTrace.Contracts/Tasks/` | stav jedné běžící úlohy v paměti scraperu včetně `CancellationTokenSource` a `Task` |
| `IScraperCommandService` | `PriceTrace.ApiService/Services/` | tenký obal nad `IPublishEndpoint` |
| `IScrapingService` / `ScrapingService` | `PriceTrace.Application/Services/` | správa smyček, deduplikace, ukládání |
| `ScrapingStartupService` | `PriceTrace.ScraperService/` | `IHostedService`, který při startu obnoví scrapování všech requestů z DB |
| `IPlaywrightService` / `PlaywrightService` | `PriceTrace.Application/Services/` | jeden Chromium a `IBrowserContext`, pro každý běh vytváří novou `IPage` |
| `BaseScraper` | `PriceTrace.Application/` | abstrakce scraperu + `PopulateUrl` |
| `BazosScraper` | `PriceTrace.Application/Scrapers/` | jediná implementace |
| `IScraperModelResult`, `BazosScraperResult` | `PriceTrace.Contracts/ScrapeModelResults/` | obal nad `List<ListingEntity>` |
| `FleaMarketWebEnum` | `PriceTrace.Common/Enums/` | `unknown = 0`, `bazos = 1` |

## BaseScraper a šablona URL

`BaseSearchUrl` je šablona s ručně nahrazovanými zástupnými znaky (není to `string.Format`):

| Zástupný znak | Hodnota |
| --- | --- |
| `{0}` | hledaný výraz – jeden prvek z `Product.Names` (URL-enkódovaný přes `Uri.EscapeDataString`) |
| `{1}` | `HardUpLimit` (horní cenový limit), `null` → prázdný řetězec |
| `{2}` | `HardDownLimit` (dolní cenový limit), `null` → prázdný řetězec |

`PopulateUrl` vrací jednu URL pro každý prvek `Product.Names`. Hledá se tedy podle `Names`,
ne podle `ProductName`; produkt s prázdným `Names` nenajde nic.

## BazosScraper

Šablona: `https://www.bazos.cz/search.php?hledat={0}&…&cenaod={2}&cenado={1}&…`

Pro každou URL: otevře stránku (`WaitUntilState.DOMContentLoaded`), zkusí zavřít cookie lištu (`#termsfeed-com---nb`), pak v
cyklu čte inzeráty a kliká na „Další“ (s prodlevou 500 ms mezi stránkami a limitem `MaxPages = 20`). Pokud stránka neobsahuje žádné inzeráty, nezasekne se na timeoutu a přejde dál.

| Údaj | Selektor |
| --- | --- |
| kontejner inzerátu | `.inzeraty.inzeratyflex` |
| titulek a odkaz | `.nadpis a` |
| cena | `.inzeratycena b` |
| popis | `.popis` |
| lokalita | `.inzeratylok` |
| další stránka | `.strankovani a:has-text('Další')` |

Cena (`BazosScraper.ParsePrice`): `"Zdarma"` se převede na `0`, `"Dohodou"` / `"V textu"` / `"Nabídněte"` na `-1`, z číselných cen se extrahují číslice a parsují přes `decimal.TryParse` s `CultureInfo.InvariantCulture`. Význam
hodnot `Price` podle `ListingEntity`: `-1` dohodou, `0` zdarma, `1+` cena v Kč. Výsledky ze
všech URL se nakonec deduplikují podle `Url`. `CreatorName` a `ViewCount` scraper neplní.

Selektory jsou svázané s aktuálním HTML Bazoše. Když scraper začne vracet prázdné výsledky, první podezřelý je změna jejich markupu.

## Soft a hard limity

- `HardUpLimit` / `HardDownLimit` jdou do URL a filtrují výsledky už na straně Bazoše.
- `SoftUpLimit` / `SoftDownLimit` se vyhodnocují v `ScrapingService` při uložení nového inzerátu (shoda s rozpočtem uživatele se zaloguje jako `[SOFT LIMIT MATCH]`).

## Instalace prohlížečů

Playwright potřebuje jednorázově stáhnout Chromium. Skript `playwright.ps1` se po buildu
objeví ve výstupní složce každého projektu, který Playwright (i tranzitivně) referencuje:

```bash
dotnet build PriceTrace.slnx
```

```bash
pwsh PriceTrace.ScraperService/bin/Debug/net10.0/playwright.ps1 install chromium
```

Skript vyžaduje PowerShell (`pwsh`), který na Linuxu nebývá předinstalovaný.

Prohlížeče se instalují do sdílené cache uživatele. Bez instalace
`scraperservice` spadne hned při startu, protože `Program.cs` volá
`playwrightService.InitializeAsync()` ještě před `host.Run()`.
