using Microsoft.Playwright;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.ScrapeModelResults;
using System.Net;

namespace PriceTrace.Application;

public abstract class BaseScraper
{
    public abstract string BaseSearchUrl { get; set; }

    public abstract Task<IScraperModelResult> GetSearchResult(IPage page, SearchRequestsEntity searchRequestsEntity);

    /// <summary>
    /// 0 - Name
    /// 1 - Hard max limit
    /// 2 - Hard min limit
    /// </summary>
    /// <param name="searchRequestsEntity"></param>
    /// <returns></returns>
    public string[] PopulateUrl(SearchRequestsEntity searchRequestsEntity)
    {
        return searchRequestsEntity.Product.Names.Select(name =>
        {
            string url = BaseSearchUrl;

            url = url.Replace("{0}", Uri.EscapeDataString(name ?? ""));
            url = url.Replace("{1}", Uri.EscapeDataString(searchRequestsEntity.HardUpLimit.ToString() ?? string.Empty));
            url = url.Replace("{2}", Uri.EscapeDataString(searchRequestsEntity.HardDownLimit.ToString() ?? string.Empty));

            return url;
        }).ToArray();
    }
}