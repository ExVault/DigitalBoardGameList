using System.Text;
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
    private readonly List<EnrichmentFailure> _failures = [];

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
            new SteamDbPatchNotesRss(RequestDelay.FromSeconds(9, 11)) // Got 429 on 7-10
        ];
    }

    public async Task<PublishCatalog> RunAsync()
    {
        var enrichContexts = CreateContextsFromCatalogs();

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

        UpdateStandaloneDlcGames(enrichContexts);

        return new PublishCatalog(ToValidDto(enrichContexts));
    }

    private List<EnrichmentContext> CreateContextsFromCatalogs()
    {
        var result = new List<EnrichmentContext>(_gameCatalog.Games.Count);

        if (_previousCatalog != null)
        {
            var previousCatalogDict = _previousCatalog.Games.ToDictionary(game => game.Bgg.Id);

            foreach (var game in _gameCatalog.Games)
            {
                var previous = previousCatalogDict.GetValueOrDefault(game.BggId);

                if (previous != null && _settings.ProcessOnlyNewGames)
                    continue;

                result.Add(new EnrichmentContext(new EnrichmentData(game), previous, _settings));
            }
        }
        else
        {
            foreach (var game in _gameCatalog.Games)
            {
                result.Add(new EnrichmentContext(new EnrichmentData(game), null, _settings));
            }
        }

        return result;
    }

    private void UpdateStandaloneDlcGames(List<EnrichmentContext> enrichContexts)
    {
        foreach (var context in enrichContexts)
        {
            if (context.CurrentData.Game.PullDataFromId == null)
                continue;

            var source = enrichContexts.Find(c => c.CurrentData.Bgg.Id == context.CurrentData.Game.PullDataFromId);
            if (source == null)
            {
                Log.Error("[{Type}] {GameName} cannot find game {SourceId} to pull data from",
                    nameof(EnrichmentProcess), context.CurrentData.Game.Name, context.CurrentData.Game.PullDataFromId);
            }
            else
            {
                context.CurrentData.PullDataFrom(source.CurrentData);
            }
        }
    }

    private List<GameDto> ToValidDto(List<EnrichmentContext> enrichContexts)
    {
        var result = new List<GameDto>();

        foreach (var context in enrichContexts)
        {
            var currentData = context.CurrentData;

            if (!currentData.HasError)
            {
                result.Add(currentData.ToDto());
                continue;
            }

            if (context.PreviousData == null)
            {
                Log.Error("[{Type}] {GameName} is removed from resulting output data due to error(s) " +
                          "during enrichment and lack of fallback data",
                    nameof(EnrichmentProcess), currentData.Game.Name);

                _failures.Add(new(currentData.Game.Name, noFallback: true));

                continue;
            }

            ApplyFallbackValues(currentData, context.PreviousData);

            result.Add(currentData.ToDto());
        }

        if (_settings.ProcessOnlyNewGames && _previousCatalog != null)
        {
            var validIds = result.Select(game => game.Bgg.Id).ToHashSet();
            result.AddRange(_previousCatalog.Games.Where(previous => !validIds.Contains(previous.Bgg.Id)));
        }

        return result;
    }

    private void ApplyFallbackValues(EnrichmentData current, GameDto previous)
    {
        var failure = new EnrichmentFailure(current.Game.Name);
        _failures.Add(failure);

        if (current.Bgg.Rank == 0)
        {
            current.Bgg.Rank = previous.Bgg.Rank;
            failure.FallbackApplied("Bgg.Rank");
        }
        if (current.Bgg.Rating == 0)
        {
            current.Bgg.Rating = previous.Bgg.Rating;
            failure.FallbackApplied("Bgg.Rating");
        }
        if (current.Game.ImageUrl == null && previous.ImageUrl != null)
        {
            current.Game.ImageUrl = previous.ImageUrl;
            failure.FallbackApplied("ImageUrl");
        }
        if (current.Game.Developer == null && previous.Developer != null)
        {
            current.Game.Developer = previous.Developer;
            failure.FallbackApplied("Developer");
        }
        if (current.Game.Publisher == null && previous.Publisher != null)
        {
            current.Game.Publisher = previous.Publisher;
            failure.FallbackApplied("Publisher");
        }
        if (current.Game.Dlcs == null && previous.Dlcs != null)
        {
            current.Game.Dlcs = previous.Dlcs;
            failure.FallbackApplied("DLCs");
        }
        if (current.KnownTotalDlcCount == 0 && previous.KnownTotalDlcCount != 0)
        {
            current.KnownTotalDlcCount = previous.KnownTotalDlcCount;
            failure.FallbackApplied("KnownTotalDlcCount");
        }

        foreach (var (platformName, price) in previous.Prices)
        {
            if (current.Platforms.TryGetValue(platformName, out var platform))
            {
                if (platform.Price == null)
                {
                    platform.Price = price;
                    failure.FallbackApplied($"{platformName}.Price");
                }
            }
        }
        foreach (var (platformName, lastUpdate) in previous.LastUpdates)
        {
            if (current.Platforms.TryGetValue(platformName, out var platform))
            {
                if (platform.LastUpdate == null)
                {
                    platform.LastUpdate = lastUpdate;
                    failure.FallbackApplied($"{platformName}.LastUpdate");
                }
            }
        }
    }

    public string MakeFailureReport()
    {
        if (_failures.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"Games failed to update: {_failures.Count}");

        for (var i = 0; i < _failures.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {_failures[i]}");
        }

        return sb.ToString();
    }

    private class EnrichmentFailure
    {
        private readonly string _gameName;
        private readonly bool _noFallback;
        private readonly List<string> _appliedFallbacks;

        public EnrichmentFailure(string gameName, bool noFallback = false)
        {
            _gameName = gameName;
            _noFallback = noFallback;
            _appliedFallbacks = [];
        }

        public void FallbackApplied(string propertyName)
        {
            _appliedFallbacks.Add(propertyName);
        }

        public override string ToString()
        {
            if (_noFallback)
                return $"{_gameName}: Removed completely due to lack of fallback data";

            if (_appliedFallbacks.Count == 0) // should never happen
                return $"{_gameName}: Error marked, but no fallback values were applied";

            return $"{_gameName}: {string.Join(", ", _appliedFallbacks)}";
        }
    }
}