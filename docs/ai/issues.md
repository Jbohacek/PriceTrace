# Problémy aplikace

Seznam zjevných chyb a nedostatků aplikace, seřazený podle závažnosti. Každá položka říká,
kde problém je, co způsobuje a jak by se dal opravit. Stav k 10. 11. 2026.

Všechno zde plyne z přečtení kódu; aplikace při sepisování neběžela. Položky označené
**[neověřeno]** jsou podezření, která je potřeba potvrdit za běhu. Když něco opravíš, položku
smaž.

Pasti pro vývojáře, které nejsou chybou aplikace (kódování souborů, překlepy v názvech,
zbytky šablony), jsou v [known-issues.md](known-issues.md).

## Přehled

| # | Problém | Závažnost |
| --- | --- | --- |
| 1 | Po restartu scraperu se hlídání samo neobnoví | kritická |
| 2 | Souběžné úlohy sdílí jednu stránku prohlížeče | kritická |
| 3 | Žádná autentizace ani autorizace | kritická |
| 4 | Cena se špatně parsuje, „Zdarma“ se ukládá jako „Dohodou“ | vysoká |
| 5 | Úprava hlídání se neprojeví v běžící úloze | vysoká |
| 6 | Smazání produktu nebo uživatele nezastaví scrapování | vysoká |
| 7 | Inzerát se uloží jen k jednomu produktu a nikdy se neaktualizuje | vysoká |
| 8 | Detail produktu počítá statistiky nejvýš ze 100 inzerátů | vysoká |
| 9 | Selhání zprávy ve scraperu se tiše zahodí | střední |
| 10 | Uložení a odeslání zprávy nejsou atomické | střední |
| 11 | Soft limity nemají žádný efekt | střední |
| 12 | Hledání ve filtrech rozlišuje velikost písmen | střední |
| 13 | Žádná validace vstupů | střední |
| 14 | `GET /api/listings` vrací celou tabulku | střední |
| 15 | Dotaz do DB pro každý nalezený inzerát, chybí index | střední |
| 16 | Migrace se spouští jen v Development | střední |
| 17 | Heslo k databázi v repozitáři | střední |
| 18 | Zranitelná závislost `MessagePack` | střední |
| 19 | Scraper nemá telemetrii a jeho logy z Bazoše mizí | nízká |
| 20 | Scraper nemá ochranu proti blokování a zaseknutí | nízká |
| 21 | Chybí testy | nízká |

## Kritické

### 1. Po restartu scraperu se hlídání samo neobnoví

- **Kde:** `PriceTrace.Application/Services/ScrapingService.cs:18` (`_runningTasks`),
  `PriceTrace.ScraperService/Program.cs`
- **Co se děje:** Běžící úlohy jsou jen ve slovníku v paměti. Start úlohy vyvolá pouze zpráva
  `StartScrapingCommand`, kterou API posílá při vytvoření hlídání. Po restartu
  `scraperservice` (nasazení, pád, vypnutí AppHostu) zůstanou hlídání v DB, ale žádné neběží.
  UI přitom nic nenaznačí.
- **Oprava:** Při startu `ScraperService` načíst všechny `SearchRequestsEntity` z DB a
  spustit je (např. `IHostedService`). Ideálně přidat endpoint se stavem úloh
  (`IScrapingService.IsRunning` už existuje, ale nikdo ho nevolá) a zobrazit ho v UI.

### 2. Souběžné úlohy sdílí jednu stránku prohlížeče

- **Kde:** `PriceTrace.Application/Services/PlaywrightService.cs:37,53`,
  `PriceTrace.Application/Services/ScrapingService.cs:124`
- **Co se děje:** `PlaywrightService` vytvoří jedinou `IPage` a vrací ji všem. Každé hlídání
  běží ve vlastním `Task.Run` a stránku používá bez zámku. Jakmile běží dvě hlídání současně,
  jedno druhému přepíše navigaci: výsledkem jsou výjimky z Playwrightu, nebo – hůř – inzeráty
  jednoho produktu uložené k jinému produktu.
- **Oprava:** Pro každý běh vytvořit vlastní stránku (`_context.NewPageAsync()`) a po
  dokončení ji zavřít; případně serializovat běhy přes `SemaphoreSlim`.

### 3. Žádná autentizace ani autorizace

- **Kde:** všechny controllery v `PriceTrace.ApiService/Controllers/`,
  `PriceTrace.Web/Components/Pages/SearchRequests.razor`
- **Co se děje:** API je zcela otevřené – kdokoli může číst, měnit a mazat data všech
  uživatelů a spouštět scrapování. Ve webu se „přihlášený“ uživatel vybírá z rozbalovacího
  seznamu. `UserEntity` má jen `Username`.
- **Oprava:** Pro PoC přijatelné, ale před jakýmkoli zveřejněním je nutná identita
  (ASP.NET Core Identity / OIDC) a filtrování dat podle přihlášeného uživatele.

## Vysoké

### 4. Cena se špatně parsuje, „Zdarma“ se ukládá jako „Dohodou“

- **Kde:** `PriceTrace.Application/Scrapers/BazosScraper.cs:84-88`
- **Co se děje:** Po odstranění „Kč“ jde text do `double.TryParse(price, out …)` bez
  `CultureInfo`; při neúspěchu se uloží `-1`.
  - Texty „Zdarma“, „Dohodou“, „V textu“ apod. všechny skončí jako `-1` (dohodou). Hodnota
    `0` (zdarma), kterou dokumentuje `ListingEntity.Price`, ze scraperu nikdy nevzejde.
  - **[neověřeno]** Výsledek závisí na kultuře procesu. Cena s mezerou jako oddělovačem tisíců
    („5 500“) projde jen tam, kde tomu odpovídá nastavení kultury; jinde se z platné ceny
    stane `-1` a inzerát vypadne ze statistik i grafů (ty berou jen `price > 0`).
  - Cena jde zbytečně přes `double` do `decimal`.
- **Oprava:** Z textu vybrat jen číslice a parsovat `decimal.TryParse` s
  `CultureInfo.InvariantCulture`; „Zdarma“ mapovat explicitně na `0`.

### 5. Úprava hlídání se neprojeví v běžící úloze

- **Kde:** `PriceTrace.ApiService/Controllers/SearchRequestsController.cs:77-91`
- **Co se děje:** `PUT` změní řádek v DB, ale neposílá žádnou zprávu. Běžící smyčka má limity
  a interval zkopírované ve `ScrapingTask` a jede se starými hodnotami až do restartu.
  Uživatel v UI vidí nové hodnoty a nemá jak poznat, že neplatí.
- **Oprava:** Po uložení poslat `StopScrapingCommand` a `StartScrapingCommand`, nebo nechat
  smyčku v každé iteraci načíst aktuální `SearchRequestsEntity` z DB (to by zároveň
  zjednodušilo `ScrapingTask`).

### 6. Smazání produktu nebo uživatele nezastaví scrapování

- **Kde:** `PriceTrace.ApiService/Controllers/ProductsController.cs:58`,
  `PriceTrace.ApiService/Controllers/UsersController.cs:58`
- **Co se děje:** Kaskáda v DB smaže navázaná hlídání, ale `StopScrapingCommand` se pošle jen
  při mazání samotného hlídání. Smyčky smazaných hlídání dál v intervalu chodí na Bazoš,
  stáhnou všechny stránky výsledků a teprve pak zjistí, že produkt neexistuje.
- **Oprava:** Před smazáním načíst navázaná hlídání a pro každé poslat stop; nebo ve smyčce
  ověřit existenci hlídání před scrapováním a při neexistenci se ukončit.

### 7. Inzerát se uloží jen k jednomu produktu a nikdy se neaktualizuje

- **Kde:** `PriceTrace.Application/Services/ScrapingService.cs:175`
- **Co se děje:** Existence se testuje jen podle `Url` napříč celou tabulkou.
  - Inzerát odpovídající dvěma produktům se uloží jen k tomu, který ho našel první.
  - Existující inzerát se přeskočí celý – změna ceny, titulku ani popisu se nezaznamená.
    Aplikace, která se jmenuje PriceTrace, tak vývoj ceny inzerátu vůbec nesleduje; graf
    „Price Over Time“ ukazuje ceny různých inzerátů seřazené podle data nalezení.
  - Nepozná se ani, že inzerát z Bazoše zmizel (prodáno).
- **Oprava:** Deduplikovat podle dvojice (produkt, URL), při změně ceny zapsat záznam do
  historie cen (nová entita) a evidovat „naposledy viděno“.

### 8. Detail produktu počítá statistiky nejvýš ze 100 inzerátů

- **Kde:** `PriceTrace.Web/Components/Pages/ProductDetail.razor:331`,
  `PriceTrace.ApiService/Controllers/ListingsController.cs:155`
- **Co se děje:** Stránka žádá `PageSize: 1000`, API velikost stránky mlčky ořízne na 100.
  Průměr, minimum, maximum i oba grafy se spočítají z prvních 100 inzerátů, aniž by to bylo
  kdekoli vidět.
- **Oprava:** Statistiky počítat na serveru vlastním endpointem (agregace v SQL), nebo ve webu
  dočítat všechny stránky. Ořez `PageSize` by měl být poznat z odpovědi – ta už `pageSize`
  vrací, web ho jen ignoruje.

## Střední

### 9. Selhání zprávy ve scraperu se tiše zahodí

- **Kde:** `PriceTrace.ScraperService/Consumers/StartScrapingCommandConsumer.cs:53`,
  `StopScrapingCommandConsumer.cs:24`
- **Co se děje:** Oba consumery chytají všechny výjimky a jen je zalogují. MassTransit tak
  zprávu považuje za úspěšně zpracovanou – žádný retry, žádná error fronta. Když je třeba DB
  chvíli nedostupná, hlídání se nespustí a nikdo se to nedozví.
- **Oprava:** Výjimky nepolykat a nastavit retry (`UseMessageRetry`).

### 10. Uložení a odeslání zprávy nejsou atomické

- **Kde:** `PriceTrace.ApiService/Controllers/SearchRequestsController.cs:67-71` a `:101-104`
- **Co se děje:** Nejdřív `SaveChangesAsync`, potom `Publish`. Když RabbitMQ selže, `POST`
  vrátí 500, ale hlídání v DB zůstane a neběží; u `DELETE` je řádek pryč a smyčka běží dál.
- **Oprava:** Transakční outbox MassTransitu (`AddEntityFrameworkOutbox`), nebo řešení z
  bodu 1 a 6, kdy scraper stav odvozuje z DB.

### 11. Soft limity nemají žádný efekt

- **Kde:** `PriceTrace.Application/BaseScraper.cs`, `ScrapingService.cs`
- **Co se děje:** `SoftUpLimit` a `SoftDownLimit` jdou zadat v UI, ukládají se a posílají ve
  zprávě, ale nic je nevyhodnocuje. Úvodní stránka je přitom popisuje jako funkci.
- **Oprava:** Rozhodnout, co mají znamenat (zvýraznění v UI, upozornění), a doimplementovat,
  nebo je z UI dočasně odstranit.

### 12. Hledání ve filtrech rozlišuje velikost písmen

- **Kde:** `PriceTrace.ApiService/Controllers/ListingsController.cs:112-127`
- **Co se děje:** `string.Contains` se v Npgsql překládá na porovnání citlivé na velikost
  písmen. Hledání „steam“ nenajde inzerát „Steam Deck“.
- **Oprava:** `EF.Functions.ILike(l.Title, $"%{term}%")`.

### 13. Žádná validace vstupů

- **Kde:** DTO v `PriceTrace.Contracts/Models/`, controllery
- **Co se děje:** Projde cokoli, mimo jiné:
  - `IntervalInSeconds` 0 – smyčka bez pauzy bombarduje Bazoš; záporný – `Task.Delay`
    po každém běhu vyhodí výjimku,
  - `HardDownLimit` větší než `HardUpLimit`,
  - produkt s prázdným `Names` – hlídání běží, ale nikdy nic nehledá (hledá se podle `Names`,
    ne podle `ProductName`),
  - prázdné `Username` nebo `ProductName`, neexistující hodnota `FleaMarketWeb`
    (včetně `unknown = 0`, na které scraper v každé iteraci spadne).
- **Oprava:** Data annotations nebo FluentValidation na request recordech; rozumné minimum
  intervalu (např. 60 s).

### 14. `GET /api/listings` vrací celou tabulku

- **Kde:** `PriceTrace.ApiService/Controllers/ListingsController.cs:14`; totéž u ostatních
  `GetAll`
- **Co se děje:** Bez stránkování, s `Include(Product)` a s trackingem. Tabulka inzerátů roste
  s každým během scraperu.
- **Oprava:** Stránkovat, případně endpoint zrušit ve prospěch `/filter`; pro čtení
  `AsNoTracking()`.

### 15. Dotaz do DB pro každý nalezený inzerát, chybí index

- **Kde:** `PriceTrace.Application/Services/ScrapingService.cs:175`
- **Co se děje:** Pro každý nalezený inzerát jeden `SELECT` podle `Url`, nad sloupcem bez
  indexu. Protože chybí i unikátní omezení, mohou dvě souběžné úlohy vložit stejnou URL dvakrát.
- **Oprava:** Načíst existující URL jedním dotazem (`WHERE Url = ANY(...)`) a přidat unikátní
  index (podle rozhodnutí v bodě 7 nad `Url`, nebo nad dvojicí produkt + URL).

### 16. Migrace se spouští jen v Development

- **Kde:** `PriceTrace.ApiService/Program.cs:63-72`
- **Co se děje:** `MigrateDatabaseAsync()` je uvnitř `IsDevelopment()`. V jiném prostředí
  vznikne prázdná databáze bez schématu a API padá na prvním dotazu. Stejně tak se jen v
  Development mapuje `/health`, na které čeká Aspire.
- **Oprava:** Vyčlenit migrace do samostatného kroku nasazení (migrační služba v AppHostu).

### 17. Heslo k databázi v repozitáři

- **Kde:** `PriceTrace.AppHost/appsettings.json:10`, záložní connection stringy v
  `appsettings.json` API a scraperu
- **Co se děje:** Heslo `postgres` je commitnuté a Postgres je vystavený na pevném portu 5432.
- **Oprava:** Parametr přesunout do user secrets, ze souborů odstranit.

### 18. Zranitelná závislost `MessagePack`

- **Kde:** `PriceTrace.AppHost` (tranzitivně přes Aspire hosting)
- **Co se děje:** Build hlásí varování NU1902/NU1903 – `MessagePack` 2.5.192 má známé
  zranitelnosti střední a vysoké závažnosti. Možná souvisí s tím, že `Aspire.AppHost.Sdk` je
  ve verzi 9.5.0, zatímco hostingové balíčky 13.1.0 **[neověřeno]**.
- **Oprava:** Sjednotit verzi Aspire SDK s balíčky; pokud varování nezmizí, přidat přímou
  referenci na opravenou verzi `MessagePack`.

## Nízké

### 19. Scraper nemá telemetrii a jeho logy z Bazoše mizí

- **Kde:** `PriceTrace.ScraperService/Program.cs`, `ScrapingService.cs:130`
- **Co se děje:** `ScraperService` nevolá `AddServiceDefaults()`, takže neposílá trasy ani
  metriky. `BazosScraper` se navíc vytváří jako `new BazosScraper()` bez loggeru, takže jeho
  logy o navigaci a stránkování se nikam nepíšou.
- **Oprava:** Přidat referenci na `ServiceDefaults` a scraper vytvářet přes DI.

### 20. Scraper nemá ochranu proti blokování a zaseknutí

- **Kde:** `PriceTrace.Application/Scrapers/BazosScraper.cs:53-110`
- **Co se děje:** Stránkuje se bez horní meze a bez prodlevy mezi stránkami; obecný výraz s
  tisíci výsledky znamená dlouhý běh a riziko zablokování. Selektory jsou natvrdo v kódu, takže
  změna HTML Bazoše shodí každou iteraci na timeoutu `WaitForSelectorAsync` – stejně tak
  dotaz s nulovým počtem výsledků.
- **Oprava:** Limit počtu stránek, krátká prodleva, rozlišit „žádné výsledky“ od chyby.

### 21. Chybí testy

- **Kde:** `PriceTrace.Tests/WebTests.cs`
- **Co se děje:** Jediný test ověřuje, že úvodní stránka vrátí 200, a potřebuje k tomu Docker
  a celý AppHost. Parsování ceny, skládání URL, mapování ani controllery nejsou pokryté.
- **Oprava:** Začít jednotkovými testy pro `BaseScraper.PopulateUrl` a parsování ceny
  (to je potřeba nejdřív vytáhnout z `BazosScraper` do samostatné metody).
