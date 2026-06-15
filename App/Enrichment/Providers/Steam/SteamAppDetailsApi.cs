using System.Text.Json;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.Steam;

public class SteamAppDetailsApi : CommonGameEnricher
{
    public SteamAppDetailsApi(RequestDelay delay) : base(delay)
    {
    }

    protected override string PlatformName => Platform.Names.Steam;

    protected override async Task EnrichSingleAsync(EnrichmentContext context, string gameId)
    {
        var currentData = context.CurrentData;

        Log.Debug("[{Type}] Going to get price data for {GameName}", nameof(SteamAppDetailsApi), currentData.Game.Name);

        var response = await GetStringAsync(UrlHelper.SteamAppDetailsApi(gameId));

        Parse(response, currentData, gameId, out var dlcData);

        if (dlcData != null)
        {
            currentData.KnownTotalDlcCount += dlcData.DlcIds.Count;

            if (ShouldUpdateDlcs(context))
            {
                await UpdateDlcsAsync(dlcData, currentData);
            }
            else
            {
                currentData.Game.Dlcs = context.PreviousData?.Dlcs;
            }
        }
    }

    private static void Parse(string response, EnrichmentData enrich, string id, out DlcData? dlcData)
    {
        dlcData = null;

        using var doc = JsonDocument.Parse(response);

        var data = doc.RootElement.GetProperty(id).GetProperty("data");

        var isFree = data.GetProperty("is_free").GetBoolean();
        if (isFree)
        {
            enrich.Platforms[Platform.Names.Steam].Price = 0;
        }
        else
        {
            var priceOverview = data.GetProperty("price_overview");
            var priceFinal = priceOverview.GetProperty("final").GetDecimal();
            //var priceInitial = priceOverview.GetProperty("initial").GetDecimal();

            var price = priceFinal / 100;

            Log.Debug("[{Type}] Assigning price {Price} to {GameName} on {Platform}",
                nameof(SteamAppDetailsApi), price, enrich.Game.Name, Platform.Names.Steam);

            enrich.Platforms[Platform.Names.Steam].Price = price;
        }

        if (enrich.Game.Developer == null)
        {
            var dev = data.GetProperty("developers").EnumerateArray().FirstOrDefault().GetString()!;

            dev = Util.CleanDeveloperName(dev);

            Log.Debug("[{Type}] Assigning developer {Developer} to {GameName}",
                nameof(SteamAppDetailsApi), dev, enrich.Game.Name);

            enrich.Game.Developer = dev;
        }
        if (enrich.Game.Publisher == null)
        {
            var pub = data.GetProperty("publishers").EnumerateArray().FirstOrDefault().GetString()!;

            pub = Util.CleanDeveloperName(pub);

            Log.Debug("[{Type}] Assigning publisher {Publisher} to {GameName}",
                nameof(SteamAppDetailsApi), pub, enrich.Game.Name);

            enrich.Game.Publisher = pub;
        }

        // Prioritize images from AppStore
        if (enrich.Game.ImageUrl == null && !enrich.Platforms.ContainsKey(Platform.Names.AppStore))
        {
            enrich.Game.ImageUrl = data.GetProperty("header_image").GetString();
        }

        if (data.TryGetProperty("dlc", out var dlcList))
        {
            var dlcIds = new List<string>();

            foreach (var dlc in dlcList.EnumerateArray())
            {
                dlcIds.Add(dlc.GetRawText());
            }

            if (dlcIds.Count > 0) // Can dlc list be empty?
            {
                var fullName = data.GetProperty("name").GetString()!;
                dlcData = new DlcData(fullName, dlcIds);
            }
        }
    }

    private async Task UpdateDlcsAsync(DlcData dlcData, EnrichmentData enrich)
    {
        var dlcs = new List<string>(dlcData.DlcIds.Count);

        foreach (var dlcId in dlcData.DlcIds)
        {
            var response = await GetStringAsync(UrlHelper.SteamAppDetailsApi(dlcId));

            var dlcName = ParseDlcName(response, dlcId, dlcData.FullGameName);

            if (dlcName == null)
                continue;

            Log.Debug("[{Type}] Adding {Dlc} to {GameName} DLCs",
                nameof(SteamAppDetailsApi), dlcName, enrich.Game.Name);

            dlcs.Add(dlcName);
        }
        if (enrich.Game.Dlcs != null)
        {
            dlcs.AddRange(enrich.Game.Dlcs);
        }
        enrich.Game.Dlcs = dlcs;
    }

    private static string? ParseDlcName(string response, string id, string fullGameName)
    {
        using var doc = JsonDocument.Parse(response);

        var data = doc.RootElement.GetProperty(id).GetProperty("data");
        var name = data.GetProperty("name").GetString()!;

        return Util.CleanDlcName(name, fullGameName);
    }

    private static bool ShouldUpdateDlcs(EnrichmentContext context)
    {
        if (context.Settings.ForceDlcUpdate)
            return true;

        if (context.PreviousData == null)
            return true;

        return context.CurrentData.KnownTotalDlcCount != context.PreviousData.KnownTotalDlcCount;
    }

    private class DlcData
    {
        public DlcData(string fullGameName, List<string> dlcIds)
        {
            FullGameName = fullGameName;
            DlcIds = dlcIds;
        }

        public string FullGameName { get; }
        public List<string> DlcIds { get; }
    }
}