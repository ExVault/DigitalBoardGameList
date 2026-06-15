using DigitalBoardGameList.App.Catalog.Output;

namespace DigitalBoardGameList.App.Enrichment;

public class EnrichmentContext
{
    public EnrichmentData CurrentData { get; }
    public GameDto? PreviousData { get; }
    public AppSettings Settings { get; }

    public EnrichmentContext(EnrichmentData currentData, GameDto? previousData, AppSettings settings)
    {
        CurrentData = currentData;
        PreviousData = previousData;
        Settings = settings;
    }
}