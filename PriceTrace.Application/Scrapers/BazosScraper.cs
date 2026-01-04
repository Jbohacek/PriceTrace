using Microsoft.Playwright;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.ScapeModelResults;
using System.Net;
using static System.Net.WebRequestMethods;

namespace PriceTrace.Application.Scrapers;

public class BazosScraper : BaseScraper
{
    public override string BaseSearchUrl { get; set; } =
        "https://www.bazos.cz/search.php?hledat={0}&rubriky=www&hlokalita=&humkreis=&cenaod={1}&cenado={2}&Submit=Hledat&order=&kitx=ano";

    public override async Task<IScaperModelResult> GetSearchResult(IPage page, SearchRequestsEntity searchRequestsEntity)
    {
        var urls = base.PopulateUrl(searchRequestsEntity);

        var result = new List<ListingEntity>();

        foreach (var t in urls)
        {
            result.AddRange(await ProcessOnePage(page, t, searchRequestsEntity.Product));
        }

        return new BazosScraperResult() {Result = result};
    }

    private async Task<ICollection<ListingEntity>> ProcessOnePage(IPage page, string url, ProductEntity product)
    {
        var allResults = new List<ListingEntity>();
        Console.WriteLine($"Naviguji na počáteční URL: {url}");

        await page.GotoAsync(url);
        bool hasNextPage = true;

        while (hasNextPage)
        {
            await page.WaitForSelectorAsync(".inzeraty.inzeratyflex");

            var listingLocators = page.Locator(".inzeraty.inzeratyflex");
            var count = await listingLocators.CountAsync();

            Console.WriteLine($"Načítám {count} inzerátů z aktuální stránky...");

            for (int i = 0; i < count; i++)
            {
                var item = listingLocators.Nth(i);

                // Extrakce dat
                var titleElement = item.Locator(".nadpis a");
                var title = await titleElement.InnerTextAsync();
                var link = await titleElement.GetAttributeAsync("href");

                var priceRaw = await item.Locator(".inzeratycena b").InnerTextAsync();
                var description = await item.Locator(".popis").InnerTextAsync();
                var locationRaw = await item.Locator(".inzeratylok").InnerTextAsync();

                // Ošetření odkazu (přidání domény, pokud chybí)
                var absoluteLink = link?.StartsWith("http") == true ? link : $"https://www.bazos.cz{link}";

                var price = priceRaw.Replace("Kč", string.Empty).Trim();

                var suscces = double.TryParse(price, out double priceValue);

                if (suscces == false)
                {
                    priceValue = -1;
                }

                allResults.Add(new ListingEntity
                {
                    Title = title.Trim(),
                    Url = absoluteLink,
                    Price = Convert.ToDecimal(priceValue), // Bazoš používá pevné mezery
                    Description = description.Trim(),
                    Location = locationRaw.Replace("\n", " ").Trim(),
                    Product = product,
                });
            }

            // --- Logika stránkování ---
            var nextButton = page.Locator(".strankovani a:has-text('Další')");

            if (await nextButton.CountAsync() > 0)
            {
                Console.WriteLine("Přecházím na další stránku...");

                // Klikneme a počkáme na načtení sítě
                await nextButton.First.ClickAsync();

                // NetworkIdle je fajn, ale jistější je počkat na změnu obsahu nebo krátký delay, 
                // protože Bazoš je starší web a NetworkIdle nemusí vždy reagovat přesně.
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            else
            {
                Console.WriteLine("Dosažena poslední stránka.");
                hasNextPage = false;
            }
        }

        return allResults;
    }
}