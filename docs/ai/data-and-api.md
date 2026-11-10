# Data, API a web

## Entity (`PriceTrace.Common`)

Všechny dědí z `BaseEntity`: `Id` (`Guid.CreateVersion7()`), `CreatedDate`, `UpdatedDate`.

| Entita | Tabulka | Důležitá pole |
| --- | --- | --- |
| `ProductEntity` | `Products.tbProduct` | `ProductName`, `ProductDescription`, `Names` (`text[]`, hledané výrazy), `ImageUrl` |
| `ListingEntity` | `Products.tbListing` | `Product`, `Url`, `Price` (`-1` dohodou, `0` zdarma), `Title`, `Description`, `Location`, `CreatorName`, `ViewCount`, `RetrievedAt` |
| `EshopPriceEntity` | `Products.tbEshopPrice` | `Product`, `Price`, `Url` – nepoužívá se |
| `UserEntity` | `User.tbUser` | `Username` |
| `SearchRequestsEntity` | `User.tbSearchRequest` | `User`, `Product`, `FleaMarketWebEnum`, `Soft/Hard Up/Down Limit`, `IntervalInSeconds` (výchozí 300) |
| `LogEntity` | `Logging.tbLogs` | `Level`, `Name`, `Originator`, `Message` – nepoužívá se |

Vztahy (`PriceTraceDbContext.OnModelCreating`), vše s kaskádovým mazáním:

```
User 1 ── N SearchRequest N ── 1 Product 1 ── N Listing
                                         1 ── N EshopPrice
```

Smazání produktu tedy smaže jeho inzeráty i search requesty; smazání uživatele smaže jeho
search requesty. Cizí klíče jsou stínové vlastnosti (`ProductId`, `UserId`) – entity mají jen
navigace, které jsou `required`. V dotazech proto filtruj přes navigaci
(`l.Product.Id == id`), ne přes neexistující `l.ProductId`.

Časová razítka: `PriceTraceDbContext.SaveChanges[Async]` nastaví `CreatedDate` při vložení a
`UpdatedDate` při změně na `DateTime.UtcNow`. Řádky `x.UpdatedDate = DateTime.Now` v
controllerech jsou tím přepsané a nemají efekt.

## Repozitáře a Unit of Work (`PriceTrace.DataEf`)

`IUnitOfWork` zpřístupňuje `Products`, `Listings`, `EshopPrices`, `Users`, `SearchRequests`,
`Logs` a `SaveChangesAsync`. Všechny repozitáře dědí z generického `Repository<TEntity>`
(`GetByIdAsync`, `GetAllAsync`, `FindAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`,
`Add*`, `Update*`, `Remove*`, `Query()`).

Načítání navigací:

- `SearchRequestsRepository` přepisuje čtecí metody a vždy přidává `Include(User)` a
  `Include(Product)`. Jeho `Query()` ale navigace nenačítá.
- Ostatní repozitáře navigace nenačítají. `ListingsController` si proto bere
  `Query().Include(l => l.Product)`. `ListingMappingProfile` čte `src.Product.Id`, takže
  mapování inzerátu načteného bez `Include` spadne na `NullReferenceException`.

Dotazy jsou trackované (žádné `AsNoTracking`). Transakční metody `IUnitOfWork` existují, ale
nikde se nevolají.

## Migrace

Jediná migrace je `20260103160729_InitialCreate`. Nová:

```bash
dotnet ef migrations add <Nazev> --project PriceTrace.DataEf --startup-project PriceTrace.ApiService
```

Design-time připojení se bere z `ConnectionStrings:pricetrace` v
`PriceTrace.ApiService/appsettings.json` (lokální Postgres, DB `postgres`). Ručně
`dotnet ef database update` obvykle pouštět nemusíš – migrace aplikuje `ApiService` při startu
v Development. `ScraperService` migrace nespouští; na schéma spoléhá až ve chvíli, kdy mu
přijde zpráva.

## REST API (`PriceTrace.ApiService`)

Všechny controllery: `[ApiController]`, `api/[controller]`, žádná autorizace.

| Controller | Endpointy |
| --- | --- |
| `UsersController` | `GET /api/users`, `GET/PUT/DELETE /api/users/{id}`, `POST /api/users` |
| `ProductsController` | `GET /api/products`, `GET/PUT/DELETE /api/products/{id}`, `POST /api/products` |
| `ListingsController` | CRUD jako výše + `POST /api/listings/filter` |
| `SearchRequestsController` | CRUD + `GET /api/searchrequests/by-user/{userId}`; `POST` publikuje start, `DELETE` publikuje stop |
| `ScraperController` | `POST /api/scraper/start/{searchRequestId}`, `POST /api/scraper/stop/{searchRequestId}` |

`POST /api/listings/filter` přijímá `ListingFilterRequest` (filtry `ProductId`, `MinPrice`,
`MaxPrice`, `Location`, `CreatorName`, `RetrievedAfter/Before`, `SearchTerm`; řazení `SortBy`
∈ `price | title | retrievedat | location | viewcount`, jinak `CreatedDate`; stránkování).
`PageSize` se ořezává na 1–100. Vrací anonymní objekt
`{ totalCount, pageNumber, pageSize, totalPages, items }`; jeho protějšek na straně webu je
ručně udržovaný `FilteredListingsResponse` v `PriceTrace.Web/ListingApiClient.cs`. Když změníš
jedno, změň i druhé.

Enumy se serializují jako čísla (`"fleaMarketWeb": 1`), žádný `JsonStringEnumConverter`
není registrovaný.

Hotové ukázkové požadavky jsou v `PriceTrace.ApiService/PriceTrace.ApiService.http`
(pozor, první požadavek na `/weatherforecast` je zbytek šablony a vrátí 404).

### AutoMapper profily (`PriceTrace.ApiService/Mappings/`)

- `Entity → Response`: `ConstructUsing(...)` s pozičním konstruktorem recordu.
- `CreateRequest → Entity`: navigace (`Product`, `User`) jsou `Ignore()` a controller je
  doplní ručně po načtení z DB.
- `UpdateRequest → Entity`: `ForAllMembers(Condition(srcMember != null))`, tedy `null`
  znamená „neměnit“. U nenulovatelných hodnotových typů se přepíše vždy.
- `SearchRequestsEntity → StartScrapingCommand` v `ScraperMappingProfile`.

Přidáš-li pole do Response recordu, musíš rozšířit i `ConstructUsing` v profilu – jinak se
projekt nepřeloží.

## Web (`PriceTrace.Web`)

Blazor Web App, interaktivní stránky mají `@rendermode InteractiveServer`.

| Stránka | Route | Co dělá |
| --- | --- | --- |
| `Home.razor` | `/` | statická úvodní stránka |
| `Products.razor` | `/products` | CRUD produktů |
| `ProductDetail.razor` | `/products/{productId:guid}` | detail, statistiky a grafy cen (Chart.js) |
| `Listings.razor` | `/listings` | filtrovaný a stránkovaný seznam inzerátů |
| `SearchRequests.razor` | `/searchrequests` | CRUD hlídacích požadavků |
| `Counter.razor`, `Error.razor` | `/counter`, `/Error` | zbytky šablony |

- Volání API jdou výhradně přes `ProductApiClient`, `ListingApiClient`,
  `SearchRequestApiClient`, `UserApiClient` v kořeni projektu `Web`. Klienti po neúspěchu
  volají `EnsureSuccessStatusCode()`; stránky výjimky chytají a zobrazují `errorMessage`.
- Stránky drží stav i logiku v `@code` bloku, nemají code-behind ani sdílené komponenty.
- Bootstrap 5.3.8, Bootstrap Icons a Chart.js se načítají z CDN v `Components/App.razor`.
  Lokální kopie Bootstrapu ve `wwwroot/lib/` je zakomentovaná a nepoužívá se.
- Grafy: `wwwroot/js/productDetailCharts.js` vystavuje `window.productDetailCharts`
  (`initializePriceDistribution`, `initializeTimeline`, `destroy`), volané přes `IJSRuntime`.
  Očekává canvasy `priceChartCanvas` a `timelineChartCanvas` a camelCase vlastnosti
  `price`, `retrievedAt`.
- Navigace je v `Components/Layout/NavMenu.razor`.
