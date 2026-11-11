using System.ComponentModel.DataAnnotations;
using PriceTrace.Application.Scrapers;
using PriceTrace.Common.Enums;
using PriceTrace.Common.Products;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Models.ProductModels;
using PriceTrace.Contracts.Models.SearchRequestModels;

namespace PriceTrace.Tests;

public class ScraperAndValidationTests
{
    [Theory]
    [InlineData("5 500 Kč", 5500)]
    [InlineData("5\u00A0500 Kč", 5500)]
    [InlineData("12000 Kč", 12000)]
    [InlineData("0 Kč", 0)]
    [InlineData("Zdarma", 0)]
    [InlineData("zdarma", 0)]
    [InlineData("Dohodou", -1)]
    [InlineData("V textu", -1)]
    [InlineData("Nabídněte", -1)]
    [InlineData("", -1)]
    [InlineData(null, -1)]
    public void ParsePrice_ReturnsExpectedDecimalValue(string? rawPrice, decimal expected)
    {
        var actual = BazosScraper.ParsePrice(rawPrice);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PopulateUrl_GeneratesEscapedUrlForEachProductName()
    {
        var scraper = new BazosScraper();
        var searchRequest = new SearchRequestsEntity
        {
            FleaMarketWebEnum = FleaMarketWebEnum.bazos,
            HardDownLimit = 5000,
            HardUpLimit = 15000,
            Product = new ProductEntity
            {
                ProductName = "Steam Deck",
                ProductDescription = "Handheld",
                Names = ["Steam Deck", "steamdeck oled"]
            },
            User = new UserEntity
            {
                Username = "Admin"
            }
        };

        var urls = scraper.PopulateUrl(searchRequest);

        Assert.Equal(2, urls.Length);
        Assert.Contains("hledat=Steam%20Deck", urls[0]);
        Assert.Contains("cenaod=5000", urls[0]);
        Assert.Contains("cenado=15000", urls[0]);
        Assert.Contains("hledat=steamdeck%20oled", urls[1]);
    }

    [Fact]
    public void CreateSearchRequestRequest_InvalidLimitsAndWeb_FailsValidation()
    {
        var request = new CreateSearchRequestRequest(
            UserId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            FleaMarketWeb: FleaMarketWebEnum.unknown,
            HardUpLimit: 1000,
            HardDownLimit: 5000,
            IntervalInSeconds: 60);

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void CreateSearchRequestRequest_InvalidInterval_FailsValidation()
    {
        var request = new CreateSearchRequestRequest(
            UserId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            FleaMarketWeb: FleaMarketWebEnum.bazos,
            IntervalInSeconds: 5);

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Single(results);
    }

    [Fact]
    public void CreateProductRequest_EmptyNames_FailsValidation()
    {
        var request = new CreateProductRequest(
            ProductName: "Steam Deck",
            ProductDescription: "Handheld",
            Names: ["  "]);

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(isValid);
    }
}
