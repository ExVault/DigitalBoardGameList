using System.Text.Json;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.Steam;

public class SteamNewsApi : CommonGameEnricher
{
    public SteamNewsApi(RequestDelay delay) : base(delay)
    {
    }

    private static readonly string[] UpdateKeywords =
    [
        "patch", "update", "fix", "changelog", "release", "version", "build"
    ];

    private static bool LooksLikeUpdateNewsTitle(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        return UpdateKeywords.Any(kw => title.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    protected override string PlatformName => Platform.Names.Steam;

    protected override async Task EnrichSingleAsync(EnrichmentContext context, string gameId)
    {
        var currentData = context.CurrentData;
        Log.Debug("[{Type}] Updating {GameName}", nameof(SteamNewsApi), currentData.Game.Name);
        var response = await GetStringAsync(UrlHelper.SteamNewsApi(gameId));
        Parse(response, currentData);
    }

    private static void Parse(string response, EnrichmentData enrichData)
    {
        using var doc = JsonDocument.Parse(response);

        var newsEnum = doc.RootElement.GetProperty("appnews").GetProperty("newsitems").EnumerateArray();

        long maybeUpdateUnixTs = 0;

        foreach (var newsEntry in newsEnum)
        {
            if (newsEntry.TryGetProperty("tags", out var tags))
            {
                if (tags.EnumerateArray().Any(t => t.GetString() == "patchnotes"))
                {
                    var date = FromUnixSeconds(newsEntry.GetProperty("date").GetInt64());

                    Log.Debug("[{Type}] Assigning last update {Date:O} to {GameName}",
                        nameof(SteamNewsApi), date, enrichData.Game.Name);

                    enrichData.Platforms[Platform.Names.Steam].LastUpdate = date;

                    return;
                }
            }

            if (maybeUpdateUnixTs == 0 && LooksLikeUpdateNewsTitle(newsEntry.GetProperty("title").GetString()))
            {
                maybeUpdateUnixTs = newsEntry.GetProperty("date").GetInt64();
            }
        }

        // If no "patchnotes" tag was found
        if (maybeUpdateUnixTs != 0)
        {
            var date = FromUnixSeconds(maybeUpdateUnixTs);

            Log.Debug("[{Type}] Assigning last update {Date:O} to {GameName}",
                nameof(SteamNewsApi), date, enrichData.Game.Name);

            enrichData.Platforms[Platform.Names.Steam].LastUpdate = date;
        }
    }

    private static DateOnly FromUnixSeconds(long unixSeconds)
    {
        return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime);
    }
}