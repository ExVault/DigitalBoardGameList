using System.Globalization;
using System.Text.RegularExpressions;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.GooglePlay;

public class GooglePlayScraper : CommonGameEnricher
{
    private readonly Regex _priceRegex = new(
        @"itemprop=""price""[^>]*content=""\$?(\d+(?:\.\d{2})?)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Regex _updatedOnRegex = new(
        @"Updated on\s*</div>\s*<div[^>]*>\s*([^<]+)\s*</div>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Regex _discountRegex = new(
        @"(\d+(?:\.\d+)?)%\s*off</span>\s*<span[^>]*>[^<]*</span>\s*Offer ends",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public GooglePlayScraper(RequestDelay delay) : base(delay)
    {
    }

    protected override string PlatformName => Platform.Names.GooglePlay;

    protected override async Task EnrichSingleAsync(EnrichmentContext context, string gameId)
    {
        var currentData = context.CurrentData;
        Log.Information("[{Type}] Updating {GameName}", nameof(GooglePlayScraper), currentData.Game.Name);
        var response = await GetStringAsync(UrlHelper.GooglePlayEnUs(gameId));
        Parse(response, currentData);
    }

    private void Parse(string response, EnrichmentData currentData)
    {
        var priceMatch = _priceRegex.Match(response);
        if (priceMatch.Success)
        {
            var price = decimal.Parse(priceMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            decimal discount;

            var discountMatch = _discountRegex.Match(response);
            if (discountMatch.Success)
            {
                discount = decimal.Parse(discountMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                discount = 0;
            }

            Log.Debug("[{Type}] Assigning price {Price} to {GameName} on {Platform}",
                nameof(GooglePlayScraper), price, currentData.Game.Name, Platform.Names.GooglePlay);

            currentData.Platforms[Platform.Names.GooglePlay].Price = new PriceData(price, discount);
        }
        else
        {
            currentData.MarkError();
            Log.Error("[{Type}] Price regex failed ({GameName})", nameof(GooglePlayScraper), currentData.Game.Name);
        }

        var updateMatch = _updatedOnRegex.Match(response);
        if (updateMatch.Success)
        {
            var updatedOn = DateOnly.ParseExact(updateMatch.Groups[1].Value.Trim(), "MMM d, yyyy", CultureInfo.InvariantCulture);

            Log.Debug("[{Type}] Assigning last update {Date:O} to {GameName}",
                nameof(GooglePlayScraper), updatedOn, currentData.Game.Name);

            currentData.Platforms[Platform.Names.GooglePlay].LastUpdate = updatedOn;
        }
        else
        {
            currentData.MarkError();
            Log.Error("[{Type}] Last update regex failed ({GameName})", nameof(GooglePlayScraper), currentData.Game.Name);
        }
    }
}