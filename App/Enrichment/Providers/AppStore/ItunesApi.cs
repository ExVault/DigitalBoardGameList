using System.Text.Json;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.AppStore;

public class ItunesApi : TimeThrottledHttp, IGameEnricher
{
    private const int MaxIdsPerQuery = 150;

    public ItunesApi(RequestDelay delay) : base(delay)
    {
    }

    public async Task EnrichAllAsync(IEnumerable<EnrichmentContext> contexts)
    {
        var appStoreAppDict = new Dictionary<string, EnrichmentData>();

        foreach (var context in contexts)
        {
            var currentData = context.CurrentData;

            if (currentData.Game.PlatformIds.TryGetValue(Platform.Names.AppStore, out var id))
            {
                appStoreAppDict.Add(id, currentData);
            }
        }

        foreach (var chunk in appStoreAppDict.Chunk(MaxIdsPerQuery))
        {
            try
            {
                await EnrichChunkAsync(chunk, appStoreAppDict);
            }
            catch (Exception ex)
            {
                foreach (var (_, currentData) in chunk)
                {
                    currentData.MarkError();
                }
                Log.Error(ex, "[{Type}] Exception during games enrichment", nameof(ItunesApi));
            }
        }
    }

    private async Task EnrichChunkAsync(
        KeyValuePair<string, EnrichmentData>[] chunk,
        Dictionary<string, EnrichmentData> appDict)
    {
        var response = await GetStringAsync(UrlHelper.ItunesApi(chunk.Select(kvp => kvp.Key)));

        using var doc = JsonDocument.Parse(response);

        var resultCount = doc.RootElement.GetProperty("resultCount").GetInt32();

        if (resultCount != chunk.Length)
        {
            Log.Error("[{Type}] Result count {ResultCount} != {IdsCount} Requested count",
                nameof(ItunesApi), resultCount, chunk.Length);
        }

        var seenIds = new HashSet<string>();

        foreach (var result in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            if (!result.TryGetProperty("trackId", out var trackId))
            {
                Log.Error("[{Type}] Resulting json does not contain 'trackId' property", nameof(ItunesApi));
                continue;
            }

            var id = trackId.GetRawText();

            if (!appDict.TryGetValue(id, out var currentData))
            {
                Log.Error("[{Type}] Resulting json contains invalid id '{Id}'", nameof(ItunesApi), id);
                continue;
            }

            seenIds.Add(id);

            try
            {
                Parse(result, currentData);
            }
            catch (Exception ex)
            {
                currentData.MarkError();

                Log.Error(ex, "[{Type}] Exception during {GameName} game enrichment",
                    nameof(ItunesApi), currentData.Game.Name);
            }
        }

        foreach (var (id, currentData) in chunk)
        {
            if (!seenIds.Contains(id))
            {
                currentData.MarkError();

                Log.Error("[{Type}] Resulting json is missing '{Id}' id ({GameName})",
                    nameof(ItunesApi), id, currentData.Game.Name);
            }
        }
    }

    private static void Parse(JsonElement json, EnrichmentData currentData)
    {
        var price = json.GetProperty("price").GetDecimal();

        // Apple does not provide any discount info whatsoever, even on appstore frontend
        var discount = 0;

        Log.Debug("[{Type}] Assigning price {Price} to {GameName} on {Platform}",
            nameof(ItunesApi), price, currentData.Game.Name, Platform.Names.AppStore);

        var platformData = currentData.Platforms[Platform.Names.AppStore];

        platformData.Price = new PriceData(price, discount);

        var dateTime = json.GetProperty("currentVersionReleaseDate").GetDateTime();
        var date = DateOnly.FromDateTime(dateTime);

        Log.Debug("[{Type}] Assigning last update {Date:O} to {GameName} on {Platform}",
            nameof(ItunesApi), date, currentData.Game.Name, Platform.Names.AppStore);

        platformData.LastUpdate = date;

        // Prioritize developer names from Steam
        if (currentData.Game.Developer == null && !currentData.Platforms.ContainsKey(Platform.Names.Steam))
        {
            var dev = json.GetProperty("artistName").GetString()!;

            dev = Util.CleanDeveloperName(dev);

            Log.Debug("[{Type}] Assigning developer {Developer} to {GameName}",
                nameof(ItunesApi), dev, currentData.Game.Name);

            currentData.Game.Developer = dev;
        }

        currentData.Game.ImageUrl ??= json.GetProperty("artworkUrl100").GetString();
    }
}