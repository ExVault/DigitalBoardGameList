namespace DigitalBoardGameList.App.Enrichment;

public interface IGameEnricher
{
    public Task EnrichAllAsync(IEnumerable<EnrichmentContext> contexts);
}