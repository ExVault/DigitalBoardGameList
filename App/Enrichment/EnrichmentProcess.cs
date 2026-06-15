using DigitalBoardGameList.App.Catalog.Input;
using DigitalBoardGameList.App.Catalog.Output;
using DigitalBoardGameList.App.Enrichment.Providers.AppStore;
using DigitalBoardGameList.App.Enrichment.Providers.Bgg;
using DigitalBoardGameList.App.Enrichment.Providers.GooglePlay;
using DigitalBoardGameList.App.Enrichment.Providers.Steam;
using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment;

public class EnrichmentProcess
{
    private readonly GameCatalog _gameCatalog;
    private readonly PublishCatalog? _previousCatalog;
    private readonly AppSettings _settings;

    private readonly IGameEnricher[] _enrichers;

    public EnrichmentProcess(GameCatalog gameCatalog, PublishCatalog? previousCatalog, AppSettings settings)
    {
        _gameCatalog = gameCatalog;
        _previousCatalog = previousCatalog;
        _settings = settings;

        _enrichers =
        [
            new BggDataFromCsv(new LocalCsvFileLoader(settings.BggCsvPath)),
            new GooglePlayScraper(RequestDelay.FromSeconds(3, 5)),
            new ItunesApi(RequestDelay.FromSeconds(0.5)),
            new SteamAppDetailsApi(RequestDelay.FromSeconds(1.5, 2)),
            //new SteamNewsApi(RequestDelay.FromSeconds(1, 1.5)),
            new SteamDbPatchNotesRss(RequestDelay.FromSeconds(7, 10)) // Got 429 on 5-7
        ];
    }

    public async Task<PublishCatalog> RunAsync()
    {
        var gameCount = _gameCatalog.Games.Count;
        var enrichContexts = new List<EnrichmentContext>(gameCount);

        if (_previousCatalog != null)
        {
            var previousCatalogDict = _previousCatalog.Games.ToDictionary(game => game.Bgg.Id);

            foreach (var game in _gameCatalog.Games)
            {
                var previous = previousCatalogDict.GetValueOrDefault(game.BggId);

                if (previous != null && _settings.ProcessOnlyNewGames)
                    continue;

                enrichContexts.Add(new EnrichmentContext(new EnrichmentData(game), previous, _settings));
            }
        }
        else
        {
            foreach (var game in _gameCatalog.Games)
            {
                enrichContexts.Add(new EnrichmentContext(new EnrichmentData(game), null, _settings));
            }
        }

        if (enrichContexts.Count == 0)
        {
            Log.Warning("[{Type}] Nothing to process (OnlyNewGames: {OnlyNewGames})",
                nameof(EnrichmentProcess), _settings.ProcessOnlyNewGames);

            return _previousCatalog ?? new PublishCatalog([]);
        }

        if (_settings.TestRun)
        {
            enrichContexts = enrichContexts.Take(1).ToList();
        }

        await Task.WhenAll(_enrichers.Select(enricher => enricher.EnrichAllAsync(enrichContexts)));

        var validOutput = new List<GameDto>();

        foreach (var context in enrichContexts)
        {
            var currentData = context.CurrentData;

            if (!currentData.HasError)
            {
                validOutput.Add(currentData.ToDto());
                continue;
            }

            if (context.PreviousData == null)
            {
                Log.Error("[{Type}] {GameName} is removed from resulting output data due to error(s) " +
                          "during enrichment and lack of fallback data",
                    nameof(EnrichmentProcess), currentData.Game.Name);

                continue;
            }

            ApplyFallbackValues(currentData, context.PreviousData);

            validOutput.Add(currentData.ToDto());
        }

        if (_settings.ProcessOnlyNewGames && _previousCatalog != null)
        {
            var validIds = validOutput.Select(game => game.Bgg.Id).ToHashSet();
            validOutput.AddRange(_previousCatalog.Games.Where(previous => !validIds.Contains(previous.Bgg.Id)));
        }

        return new PublishCatalog(validOutput);
    }

    private static void ApplyFallbackValues(EnrichmentData enrich, GameDto previous)
    {
        enrich.Game.Dlcs ??= previous.Dlcs;
        enrich.Game.ImageUrl ??= previous.ImageUrl;
        enrich.Game.Developer ??= previous.Developer;
        enrich.Game.Publisher ??= previous.Publisher;

        if (enrich.Bgg.Rank == 0)
            enrich.Bgg.Rank = previous.Bgg.Rank;

        if (enrich.Bgg.Rating == 0)
            enrich.Bgg.Rating = previous.Bgg.Rating;

        if (enrich.KnownTotalDlcCount == 0)
            enrich.KnownTotalDlcCount = previous.KnownTotalDlcCount;

        foreach (var (platformName, price) in previous.Prices)
        {
            if (enrich.Platforms.TryGetValue(platformName, out var platform))
            {
                platform.Price ??= price;
            }
        }
        foreach (var (platformName, lastUpdate) in previous.LastUpdates)
        {
            if (enrich.Platforms.TryGetValue(platformName, out var platform))
            {
                platform.LastUpdate ??= lastUpdate;
            }
        }
    }
}