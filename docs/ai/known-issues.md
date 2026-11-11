# Pasti pro vývojáře

Věci, které v repu vypadají divně a na kterých se při práci snadno zasekneš. Slouží k tomu,
abys je neobjevoval znovu a abys je omylem „neopravil“ uprostřed jiného úkolu. Stav k
11. 11. 2026; když něco opravíš, položku smaž.

Skutečné chyby a nedostatky aplikace (scrapování, API, data, bezpečnost) jsou samostatně v
[issues.md](issues.md).

## Chování, které překvapí

- **Opakovaný start scrapování se ignoruje.** `StartScrapingAsync` pro už běžící
  `SearchRequestId` jen zaloguje varování.

## Názvy a struktura

Neměň bez výslovného zadání – přejmenování zasáhne hodně souborů.

- Entita `SearchRequestsEntity` je v množném čísle, tabulka `tbSearchRequest` v jednotném.

## Zbytky šablony a mrtvý kód

- `EshopPriceEntity`, `LogEntity` a jejich repozitáře; transakční metody a `SeedDatabase()`
  na `IUnitOfWork`; `IScrapingService.IsRunning`.

## Prostředí a nástroje

- **Konce řádků.** `.editorconfig` požaduje CRLF, `.gitattributes` má `* text=auto`. Na Linuxu
  jsou v pracovní kopii LF; nepřeváděj je hromadně.
- **Pevný port 5432** pro Postgres koliduje s lokálně běžící instancí.
- **Testy** startují celý AppHost s 30s timeouty; bez Dockeru nebo při studeném stahování
  obrazů selžou.
