using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using JetBrains.Annotations;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment.Providers.Bgg;

// https://boardgamegeek.com/data_dumps/bg_ranks
public class BggDataFromCsv : IGameEnricher
{
    private readonly IBggDataProvider _bggDataProvider;

    private Dictionary<int, CsvGameEntry>? _bggDataSource;

    public BggDataFromCsv(IBggDataProvider bggDataProvider)
    {
        _bggDataProvider = bggDataProvider;
    }

    public async Task EnrichAllAsync(IEnumerable<EnrichmentContext> contexts)
    {
        if (_bggDataSource == null)
        {
            try
            {
                _bggDataSource = await InitBggDataSource();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during BggDataSource initialization");
                foreach (var context in contexts)
                {
                    context.CurrentData.MarkError();
                }
                return;
            }
        }

        foreach (var context in contexts)
        {
            var currentData = context.CurrentData;

            if (!_bggDataSource.TryGetValue(currentData.Game.BggId, out var entry))
            {
                currentData.MarkError();

                Log.Error("[{Type}] BggDataSource does not contain {BggId} id",
                    nameof(BggDataFromCsv), currentData.Game.BggId);

                continue;
            }

            var rank = entry.Rank;
            var rating = Math.Round(entry.Average, 1);

            Log.Debug("[{Type}] Assigning Rank {Rank} / Rating {Rating} to {GameName}",
                nameof(BggDataFromCsv), rank, rating, currentData.Game.Name);

            currentData.Bgg.Rank = rank;
            currentData.Bgg.Rating = rating;
        }
    }

    private async Task<Dictionary<int, CsvGameEntry>> InitBggDataSource()
    {
        var csvData = await _bggDataProvider.LoadDataAsText();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
        };

        using var reader = new StringReader(csvData);
        using var csvReader = new CsvReader(reader, config);

        var result = new Dictionary<int, CsvGameEntry>();

        await foreach (var entry in csvReader.GetRecordsAsync<CsvGameEntry>())
        {
            result.Add(entry.Id, entry);
        }

        Log.Information("[{Type}] {EntryCount} entries loaded from CSV file", nameof(BggDataFromCsv), result.Count);

        return result;
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private struct CsvGameEntry
    {
        [Index(0)]
        public int Id { get; init; }

        [Index(3)]
        public int Rank { get; init; }

        [Index(5)]
        public double Average { get; init; }
    }
}