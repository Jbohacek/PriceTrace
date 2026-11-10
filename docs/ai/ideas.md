# Nápady na další vývoj

Návrhy funkcí, které by z proof of conceptu udělaly použitelný nástroj. Jde o názor autora
dokumentace (AI), ne o schválený plán – o prioritách rozhoduje vlastník projektu. Stav
k 10. 11. 2026.

Nápady navazují na to, co už v kódu je připravené a nevyužité (soft limity,
`EshopPriceEntity`, `LogEntity`, pole `CreatorName` a `ViewCount`, parametry lokality v URL
Bazoše). Kde nápad stojí na opravě chyby z [issues.md](issues.md), je to uvedeno.

Náročnost: **S** do jednoho dne, **M** několik dní, **L** týden a víc.

## Přehled

| # | Nápad | Přínos | Náročnost | Předpoklad |
| --- | --- | --- | --- | --- |
| 1 | Upozornění na nový inzerát | velký | M | issues 1, 2 |
| 2 | Historie ceny a stav inzerátu | velký | M | issue 7 |
| 3 | Hodnocení výhodnosti nabídky | velký | M | nápad 2, issue 4 |
| 4 | Filtr relevance (vylučovací slova, lokalita) | velký | S | – |
| 5 | Plánovač řízený databází | velký | M | – (řeší issues 1, 5, 6) |
| 6 | Stav scraperu v UI | střední | S | – |
| 7 | Detail inzerátu (prodejce, fotky, datum) | střední | M | issue 2 |
| 8 | Ceny nového zboží z e-shopů | střední | L | – |
| 9 | Další bazary | střední | M za každý | issue 2 |
| 10 | Rozpoznání varianty a stavu pomocí LLM | střední | M | nápad 7 |
| 11 | Detekce podezřelých inzerátů | střední | M | nápady 2, 3, 7 |
| 12 | Přihlášení a vlastní data uživatele | střední | L | – (řeší issue 3) |
| 13 | Práce s inzeráty: oblíbené, skryté, poznámky | malý | S | nápad 12 |
| 14 | Lepší statistiky a grafy | malý | S | issue 8 |

## Doporučené pořadí

1. **Nápad 5** spolu s opravou sdílené stránky (issue 2). Bez spolehlivého scrapování nemá
   smysl stavět nic dalšího.
2. **Nápad 4** – málo práce, okamžitě zvedne kvalitu dat.
3. **Nápad 2**, pak **3** – teprve tím aplikace začne dělat to, co má v názvu.
4. **Nápad 1** – důvod, proč by aplikaci někdo nechal běžet.

Zbytek podle chuti; nápad 12 je nutný až ve chvíli, kdy má aplikaci používat víc lidí.

## Jádro produktu

### 1. Upozornění na nový inzerát

Dnes musí uživatel sám chodit na web a dívat se. Smysl hlídání je dostat zprávu ve chvíli,
kdy se objeví zajímavá nabídka.

- Po uložení nového inzerátu publikovat událost `ListingFound` (MassTransit už tu je) a
  zpracovat ji v samostatném consumeru, který odešle zprávu.
- Tady konečně dostanou smysl **soft limity**: upozornit jen na inzeráty v rozmezí
  `SoftDownLimit`–`SoftUpLimit`, ostatní jen uložit.
- Kanály: začít jedním jednoduchým (e-mail nebo webhook na Discord/Telegram), nastavení
  kanálu uložit k uživateli.
- Pozor na první běh nového hlídání – ten najde desítky starých inzerátů a neměl by poslat
  desítky zpráv.

### 2. Historie ceny a stav inzerátu

Aplikace se jmenuje PriceTrace, ale cenu inzerátu zaznamená jednou a víc se k němu nevrátí.

- Nová entita `ListingPriceHistoryEntity` (inzerát, cena, čas); zapisovat při změně ceny.
- Na `ListingEntity` přidat `LastSeenAt` a stav (aktivní / zmizel). Inzerát, který v
  několika bězích po sobě chybí, označit jako pravděpodobně prodaný.
- Z toho plyne řada užitečných čísel: jak dlouho inzerát visel, o kolik prodejce slevil,
  za kolik se zboží reálně prodává (poslední cena zmizelých inzerátů).
- Graf „Price Over Time“ pak může ukazovat skutečný vývoj mediánu v čase.

### 3. Hodnocení výhodnosti nabídky

Úvodní stránka slibuje „digitálního experta“, který řekne, jestli je cena dobrá.

- Pro každý produkt průběžně počítat medián a kvartily z aktivních inzerátů.
- Každému inzerátu přiřadit skóre (např. o kolik procent je pod mediánem) a v seznamu ho
  ukázat jako štítek „výhodné / běžné / drahé“.
- Jakmile bude nápad 8, přidat srovnání s cenou nového kusu („62 % ceny nového“).
- Skóre se hodí i jako podmínka pro upozornění z nápadu 1.

### 4. Filtr relevance

Hledání podle názvu vrací hodně šumu: poptávky („koupím“), příslušenství, obaly, náhradní
díly, výměny.

- K hlídání přidat **vylučovací slova** a volitelně **povinná slova**; filtrovat ve
  `ScrapingService` před uložením.
- Rozumná výchozí sada vylučovacích slov („koupím“, „sháním“, „vyměním“).
- **Lokalita:** URL Bazoše už obsahuje prázdné parametry `hlokalita` (PSČ) a `humkreis`
  (okruh v km). Stačí je přidat do hlídání a do šablony URL.
- Volitelně omezit rubriku (parametr `rubriky`).

## Spolehlivost a provoz

### 5. Plánovač řízený databází

Místo nekonečných smyček v paměti nechat scraper řídit daty.

- Na `SearchRequestsEntity` přidat `IsEnabled`, `LastRunAt`, `NextRunAt`, `LastError`.
- Jeden `BackgroundService` ve `ScraperService` v krátkém intervalu vybere hlídání, kterým
  nastal čas, a postupně je zpracuje.
- Tím zmizí hned několik chyb z issues.md: přežije restart (1), úprava se projeví při
  dalším běhu (5), smazané hlídání se prostě přestane vybírat (6), a běhy jdou přirozeně
  za sebou, takže odpadá souběh nad jednou stránkou (2).
- Zprávy `Start/StopScrapingCommand` pak nejsou potřeba, nanejvýš „spusť hned“.

### 6. Stav scraperu v UI

- Na stránce hlídání ukázat: běží / neběží, poslední běh, počet nalezených a nových
  inzerátů, poslední chyba. S nápadem 5 jde jen o zobrazení sloupců.
- Tlačítka „spustit teď“ a „pozastavit“ – endpointy v `ScraperController` už existují.
- Historii běhů lze ukládat do připravené a nevyužité tabulky `Logging.tbLogs`.

### 12. Přihlášení a vlastní data uživatele

- ASP.NET Core Identity nebo externí OIDC poskytovatel; API chránit tokenem.
- Hlídání a upozornění vázat na přihlášeného uživatele, produkty mohou zůstat sdílené.
- Výběr uživatele z rozbalovacího seznamu ve `SearchRequests.razor` pak zmizí.

## Bohatší data

### 7. Detail inzerátu

Scraper čte jen výpis. Stránka inzerátu obsahuje víc: jméno prodejce, počet zobrazení,
datum vložení, fotografie, celý popis.

- Pole `CreatorName` a `ViewCount` už entita má, jen je nikdo neplní.
- Detail načítat jen pro nové inzeráty, s prodlevou – jde o jeden požadavek navíc na inzerát.
- Náhled první fotografie výrazně zlepší seznam inzerátů ve webu.

### 8. Ceny nového zboží z e-shopů

- `EshopPriceEntity` s tabulkou i repozitářem je připravená a prázdná.
- Nejjednodušší cesta je srovnávač cen (jeden zdroj místo mnoha e-shopů); cenu stačí
  obnovovat jednou denně.
- Na detailu produktu ukázat cenu nového kusu jako vodorovnou čáru v grafu.

### 9. Další bazary

- Architektura je na to připravená (`BaseScraper`, `FleaMarketWebEnum`), postup je v
  [recipes.md](recipes.md#nový-bazar-scraper).
- Kandidáti: Sbazar, Aukro, Vinted. Před implementací ověřit podmínky služby a
  `robots.txt`; kde existuje veřejné API nebo RSS, dát mu přednost před scrapováním.
- Jedno hlídání by mělo umět víc bazarů najednou (dnes je `FleaMarketWebEnum` jedna hodnota).

## Chytré funkce

### 10. Rozpoznání varianty a stavu pomocí LLM

Pod jedním názvem se prodávají různé varianty (Steam Deck 64 GB / 512 GB / OLED) za velmi
rozdílné ceny, takže společný medián moc neříká.

- Z titulku a popisu nechat jazykový model vytáhnout strukturovaná data: varianta, stav
  (nový / použitý / poškozený), zda jde vůbec o hledaný produkt, co je součástí balení.
- Volat jen pro nové inzeráty, výsledek uložit; malý a levný model stačí.
- Statistiky a hodnocení z nápadu 3 pak počítat po variantách.

### 11. Detekce podezřelých inzerátů

Úvodní stránka zmiňuje filtrování podvodů. Jednoduchá pravidla pokryjí většinu:

- cena výrazně pod mediánem,
- prodejce s mnoha inzeráty na totéž zboží,
- opakovaně mazaný a znovu vkládaný inzerát (stejný text, nová URL),
- typické fráze v popisu (platba předem, komunikace mimo platformu).

Výsledkem má být varování u inzerátu, ne jeho skrytí.

## Drobnosti v UI

### 13. Práce s inzeráty

Označit jako oblíbený, skrýt, přidat poznámku; odlišit nové inzeráty od už viděných.

### 14. Lepší statistiky a grafy

- Medián místo průměru (průměr kazí odlehlé ceny).
- Statistiky počítat na serveru, ne z jedné stránky dat.
- Inzeráty „dohodou“ a „zdarma“ zobrazovat slovně místo `-1` a `0`.
- Export seznamu do CSV.
