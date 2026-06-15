using DigitalBoardGameList.App.Network;
using Serilog;

namespace DigitalBoardGameList.App.Enrichment;

public abstract class CommonGameEnricher : TimeThrottledHttp, IGameEnricher
{
    protected CommonGameEnricher(RequestDelay delay) : base(delay)
    {
    }

    protected abstract string PlatformName { get; }
    protected abstract Task EnrichSingleAsync(EnrichmentContext context, string gameId);

    public async Task EnrichAllAsync(IEnumerable<EnrichmentContext> contexts)
    {
        foreach (var context in contexts)
        {
            await EnrichSingleSafeAsync(context);
        }
    }

    private async Task EnrichSingleSafeAsync(EnrichmentContext context)
    {
        if (!context.CurrentData.Game.PlatformIds.TryGetValue(PlatformName, out var id))
            return;

        try
        {
            await EnrichSingleAsync(context, id);
        }
        catch (Exception ex)
        {
            context.CurrentData.MarkError();
            Log.Error(ex, "Exception during \"{GameName}\" game enrichment", context.CurrentData.Game.Name);
        }
    }
}