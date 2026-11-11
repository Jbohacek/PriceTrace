using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.ScrapeModelResults;

namespace PriceTrace.Application.Scrapers;

public class BazosScraper : BaseScraper
{
    private const int MaxPages = 20;
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

    public override async Task<IScraperModelResult> GetSearchResult(IPage page, SearchRequestsEntity searchRequestsEntity)
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

    public static decimal ParsePrice(string? priceRaw)
    {
        if (string.IsNullOrWhiteSpace(priceRaw))
            return -1m;

        var trimmed = priceRaw.Trim();
        if (trimmed.Contains("Zdarma", StringComparison.OrdinalIgnoreCase))
            return 0m;

        if (trimmed.Contains("Dohodou", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("V textu", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("Nabídněte", StringComparison.OrdinalIgnoreCase))
        {
            return -1m;
        }

        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
            return -1m;

        if (decimal.TryParse(digitsOnly, NumberStyles.None, CultureInfo.InvariantCulture, out var priceValue))
            return priceValue;

        return -1m;
    }

    private async Task<ICollection<ListingEntity>> ProcessOnePage(IPage page, string url, ProductEntity product)
    {
        var allResults = new List<ListingEntity>();
        _logger?.LogInformation("Navigating to initial URL: {Url}", url);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await DismissCookieConsentAsync(page);
        bool hasNextPage = true;
        int currentPage = 0;

        while (hasNextPage && currentPage < MaxPages)
        {
            currentPage++;
            var listingLocators = page.Locator(".inzeraty.inzeratyflex");
            var count = await listingLocators.CountAsync();

            if (count == 0)
            {
                _logger?.LogInformation("No listings found on page {Page} for URL: {Url}", currentPage, url);
                break;
            }

            _logger?.LogInformation("Loading {Count} listings from page {Page}...", count, currentPage);

            for (int i = 0; i < count; i++)
            {
                var item = listingLocators.Nth(i);

                // Extract data
                var titleElement = item.Locator(".nadpis a");
                var title = await titleElement.InnerTextAsync();
                var link = await titleElement.GetAttributeAsync("href");

                var priceRaw = await item.Locator(".inzeratycena b").InnerTextAsync();
                var description = await item.Locator(".popis").InnerTextAsync();
                var locationRaw = await item.Locator(".inzeratylok").InnerTextAsync();

                var absoluteLink = link?.StartsWith("http") == true ? link : $"https://www.bazos.cz{link}";

                allResults.Add(new ListingEntity
                {
                    Title = title.Trim(),
                    Url = absoluteLink,
                    Price = ParsePrice(priceRaw),
                    Description = description.Trim(),
                    Location = locationRaw.Replace("\n", " ").Trim(),
                    Product = product,
                });
            }

            var nextButton = page.Locator(".strankovani a:has-text('Další')");

            if (currentPage < MaxPages && await nextButton.CountAsync() > 0)
            {
                _logger?.LogInformation("Moving to next page ({NextPage})...", currentPage + 1);

                await Task.Delay(500);
                await nextButton.First.ClickAsync();

                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
            else
            {
                _logger?.LogInformation("Reached last page (page {Page}).", currentPage);
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