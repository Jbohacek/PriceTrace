using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.ScapeModelResults;
using System.Net;
using static System.Net.WebRequestMethods;

namespace PriceTrace.Application.Scrapers;

public class BazosScraper : BaseScraper
{
    private readonly ILogger<BazosScraper>? _logger;

    public BazosScraper()
    {
    }

    public BazosScraper(ILogger<BazosScraper> logger)
    {
        _logger = logger;
    }

    public override string BaseSearchUrl { get; set; } =
        "https://www.bazos.cz/search.php?hledat={0}&rubriky=www&hlokalita=&humkreis=&cenaod={2}&cenado={1}&Submit=Hledat&order=&kitx=ano";

    public override async Task<IScaperModelResult> GetSearchResult(IPage page, SearchRequestsEntity searchRequestsEntity)
    {
        var urls = base.PopulateUrl(searchRequestsEntity);

        var result = new List<ListingEntity>();

        foreach (var t in urls)
        {
            result.AddRange(await ProcessOnePage(page, t, searchRequestsEntity.Product));
        }

        var distinctResults = result
            .GroupBy(x => x.Url)
            .Select(g => g.First())
            .ToList();

        _logger?.LogInformation("Scrape successful with {Count} results", distinctResults.Count);

        return new BazosScraperResult() { Result = distinctResults };
    }

    private async Task<ICollection<ListingEntity>> ProcessOnePage(IPage page, string url, ProductEntity product)
    {
        var allResults = new List<ListingEntity>();
        _logger?.LogInformation("Navigating to initial URL: {Url}", url);

        await page.GotoAsync(url);

        await DismissCookieConsentAsync(page);
        bool hasNextPage = true;

        while (hasNextPage)
        {
            await page.WaitForSelectorAsync(".inzeraty.inzeratyflex");

            var listingLocators = page.Locator(".inzeraty.inzeratyflex");
            var count = await listingLocators.CountAsync();

            _logger?.LogInformation("Loading {Count} listings from current page...", count);

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
                _logger?.LogInformation("Moving to next page...");

                // Klikneme a počkáme na načtení sítě
                await nextButton.First.ClickAsync();

                // NetworkIdle je fajn, ale jistější je počkat na změnu obsahu nebo krátký delay, 
                // protože Bazoš je starší web a NetworkIdle nemusí vždy reagovat přesně.
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            else
            {
                _logger?.LogInformation("Reached last page.");
                hasNextPage = false;
            }
        }

        return allResults;
    }

    private async Task DismissCookieConsentAsync(IPage page)
    {
        try
        {
            var cookieDialog = page.Locator("#termsfeed-com---nb");
            if (await cookieDialog.IsVisibleAsync())
            {
                var acceptButton = cookieDialog.Locator("button, a").First;
                await acceptButton.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                await Task.Delay(500);
                _logger?.LogInformation("Cookie consent dialog was closed.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to close cookie dialog (may already be closed)");
        }
    }
}