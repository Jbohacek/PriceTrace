using Microsoft.Playwright;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.ScapeModelResults;
using System.Net;

namespace PriceTrace.Application;

public abstract class BaseScraper
{
    public abstract string BaseSearchUrl { get; set; }

    public abstract Task<IScaperModelResult> GetSearchResult(IPage page, SearchRequestsEntity searchRequestsEntity);

    /// <summary>
    /// 0 - Name
    /// 1 - Hard max limit
    /// 2 - Hard min limit
    /// </summary>
    /// <param name="searchRequestsEntity"></param>
    /// <returns></returns>
    public string[] PopulateUrl(SearchRequestsEntity searchRequestsEntity)
    {
        string[] resultStrings = new string[searchRequestsEntity.Product.Names.Count()];

        for (int i = 0; i < resultStrings.Length; i++)
        {
            resultStrings[i] = BaseSearchUrl;
            resultStrings[i] = resultStrings[i].Replace("{0}", searchRequestsEntity.Product.Names[i]);
            resultStrings[i] = resultStrings[i].Replace("{1}", searchRequestsEntity.HardUpLimit.ToString());
            resultStrings[i] = resultStrings[i].Replace("{2}", searchRequestsEntity.HardDownLimit.ToString());
        }

        return resultStrings;
    }
}