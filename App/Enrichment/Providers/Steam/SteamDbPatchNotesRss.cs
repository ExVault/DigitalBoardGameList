using System.Xml.Linq;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.Steam;

public class SteamDbPatchNotesRss : CommonGameEnricher
{
    public SteamDbPatchNotesRss(RequestDelay delay) : base(delay)
    {
    }

    protected override string PlatformName => Platform.Names.Steam;

    protected override async Task EnrichSingleAsync(EnrichmentContext context, string gameId)
    {
        var currentData = context.CurrentData;
        Log.Debug("[{Type}] Going to get last update for {GameName}", nameof(SteamDbPatchNotesRss), currentData.Game.Name);
        var response = await GetStringAsync(UrlHelper.SteamDbPatchNotesRss(gameId));
        Parse(response, currentData);
    }

    private static void Parse(string response, EnrichmentData enrich)
    {
        var doc = XDocument.Parse(response);

        var pubDateStr = doc.Descendants("item").First().Element("pubDate")!.Value;

        var date = DateOnly.FromDateTime(DateTimeOffset.Parse(pubDateStr).UtcDateTime);

        Log.Debug("[{Type}] Assigning last update {Date:O} to {GameName}",
            nameof(SteamDbPatchNotesRss), date, enrich.Game.Name);

        enrich.Platforms[Platform.Names.Steam].LastUpdate = date;
    }
}