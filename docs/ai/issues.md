# Problémy aplikace

Seznam zbývajících architektonických a infrastrukturních nedostatků aplikace, seřazený podle
závažnosti. Stav k 11. 11. 2026. Když něco opravíš, položku smaž.

Pasti pro vývojáře, které nejsou chybou aplikace, jsou v [known-issues.md](known-issues.md).

## Přehled

| # | Problém | Závažnost |
| --- | --- | --- |
| 1 | Žádná autentizace ani autorizace | kritická |
| 2 | Uložení a odeslání zprávy nejsou atomické | střední |
| 3 | Heslo k databázi v repozitáři | střední |
| 4 | Zranitelná tranzitivní závislost `MessagePack` v Aspire | střední |

## Kritické

### 1. Žádná autentizace ani autorizace

- **Kde:** všechny controllery v `PriceTrace.ApiService/Controllers/`,
  `PriceTrace.Web/Components/Pages/SearchRequests.razor`
- **Co se děje:** API je zcela otevřené – kdokoli může číst, měnit a mazat data všech
  uživatelů a spouštět scrapování. Ve webu se „přihlášený“ uživatel vybírá z rozbalovacího
  seznamu. `UserEntity` má jen `Username`.
- **Oprava:** Pro PoC přijatelné, ale před jakýmkoli zveřejněním je nutná identita
  (ASP.NET Core Identity / OIDC) a filtrování dat podle přihlášeného uživatele.

## Střední

### 2. Uložení a odeslání zprávy nejsou atomické

- **Kde:** `PriceTrace.ApiService/Controllers/SearchRequestsController.cs`
- **Co se děje:** Nejdřív `SaveChangesAsync`, potom `Publish`. Když RabbitMQ selže při `POST`,
  hlídání v DB zůstane a rozběhne se až při příštím startu `ScraperService` (`ScrapingStartupService`);
  u `DELETE` se běžící smyčka sama zastaví při dalším průchodu, jakmile zjistí, že záznam v DB chybí.
- **Oprava:** Transakční outbox MassTransitu (`AddEntityFrameworkOutbox`), nebo plně databází
  řízený plánovač.

### 3. Heslo k databázi v repozitáři

- **Kde:** `PriceTrace.AppHost/appsettings.json:10`, záložní connection stringy v
  `appsettings.json` API a scraperu
- **Co se děje:** Heslo `postgres` je commitnuté a Postgres je vystavený na pevném portu 5432.
- **Oprava:** Parametr přesunout do user secrets, ze souborů odstranit.

### 4. Zranitelná tranzitivní závislost `MessagePack` v Aspire

- **Kde:** `PriceTrace.AppHost` (tranzitivně přes `Aspire.Hosting.AppHost` 13.1.0)
- **Co se děje:** Build hlásí varování NU1902/NU1903 – tranzitivní `MessagePack` 2.5.192 má
  známé zranitelnosti.
- **Oprava:** Přidat přímou referenci na novější verzi `MessagePack` při aktualizaci balíčků.
