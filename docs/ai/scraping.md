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
   (klíč = `SearchRequestId`). Každá iterace:
   - vytvoří DI scope a vezme `IUnitOfWork`,
   - vybere scraper podle `FleaMarketWeb` (`switch` v `ExecuteSingleScrapingAsync`),
   - zavolá `scraper.GetSearchResult(page, searchRequest)`,
   - pro každý výsledek hledá existující inzerát podle `Url`; nové vloží, existující přeskočí,
   - uloží a počká `IntervalInSeconds` (výchozí 300 s). Po chybě čeká 60 s a zkouší znovu.
6. **Zastavení** – `DELETE /api/searchrequests/{id}` nebo `POST /api/scraper/stop/{id}`
   publikuje `StopScrapingCommand`; consumer zavolá `StopScrapingAsync`, který zruší
   `CancellationTokenSource` a úlohu odebere ze slovníku.

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
| `IPlaywrightService` / `PlaywrightService` | `PriceTrace.Application/Services/` | jeden Chromium, jeden kontext, **jedna sdílená `IPage`** |
| `BaseScraper` | `PriceTrace.Application/` | abstrakce scraperu + `PopulateUrl` |
| `BazosScraper` | `PriceTrace.Application/Scrapers/` | jediná implementace |
| `IScaperModelResult`, `BazosScraperResult` | `PriceTrace.Contracts/ScapeModelResults/` | obal nad `List<ListingEntity>` |
| `FleaMarketWebEnum` | `PriceTrace.Common/Enums/` | `unknown = 0`, `bazos = 1` |

## BaseScraper a šablona URL

`BaseSearchUrl` je šablona s ručně nahrazovanými zástupnými znaky (není to `string.Format`):

| Zástupný znak | Hodnota |
| --- | --- |
| `{0}` | hledaný výraz – jeden prvek z `Product.Names` |
| `{1}` | `HardUpLimit` (horní cenový limit), `null` → prázdný řetězec |
| `{2}` | `HardDownLimit` (dolní cenový limit), `null` → prázdný řetězec |

`PopulateUrl` vrací jednu URL pro každý prvek `Product.Names`. Hledá se tedy podle `Names`,
ne podle `ProductName`; produkt s prázdným `Names` nenajde nic.

## BazosScraper

Šablona: `https://www.bazos.cz/search.php?hledat={0}&…&cenaod={2}&cenado={1}&…`

Pro každou URL: otevře stránku, zkusí zavřít cookie lištu (`#termsfeed-com---nb`), pak v
cyklu čte inzeráty a kliká na „Další“, dokud odkaz existuje.

| Údaj | Selektor |
| --- | --- |
| kontejner inzerátu | `.inzeraty.inzeratyflex` |
| titulek a odkaz | `.nadpis a` |
| cena | `.inzeratycena b` |
| popis | `.popis` |
| lokalita | `.inzeratylok` |
| další stránka | `.strankovani a:has-text('Další')` |

Cena: odstraní se „Kč“ a zbytek jde do `double.TryParse`. Neúspěch znamená `-1`. Význam
hodnot `Price` podle `ListingEntity`: `-1` dohodou, `0` zdarma, `1+` cena v Kč. Výsledky ze
všech URL se nakonec deduplikují podle `Url`. `CreatorName` a `ViewCount` scraper neplní.

Selektory jsou svázané s aktuálním HTML Bazoše. Když scraper začne vracet prázdné výsledky
nebo padat na `WaitForSelectorAsync`, první podezřelý je změna jejich markupu.

## Soft a hard limity

- `HardUpLimit` / `HardDownLimit` jdou do URL a filtrují výsledky už na straně Bazoše.
- `SoftUpLimit` / `SoftDownLimit` se ukládají a posílají ve zprávě, ale scraper je zatím
  nijak nepoužívá. Zamýšlené použití (podle textu na úvodní stránce) je „rozpočet uživatele“.

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

`README.md` uvádí cestu `PriceTrace.Web\bin\Debug\net10.0`; projekt `Web` ale Playwright
nereferencuje, takže tam skript není. Prohlížeče se instalují do sdílené cache uživatele,
takže na konkrétní složce nezáleží – stačí kterákoli, kde skript skutečně je. Bez instalace
`scraperservice` spadne hned při startu, protože `Program.cs` volá
`playwrightService.InitializeAsync()` ještě před `host.Run()`.

## Omezení, se kterými musíš počítat

Podrobněji v [issues.md](issues.md):

- Běžící úlohy žijí jen v paměti. Po restartu `scraperservice` se nic samo nerozběhne.
- Úprava search requestu (`PUT`) běžící úlohu neovlivní.
- Všechny úlohy sdílí jednu `IPage`; souběžné běhy si navzájem přepisují navigaci.
- Deduplikace je podle `Url` napříč všemi produkty a existující inzerát se nikdy neaktualizuje.
