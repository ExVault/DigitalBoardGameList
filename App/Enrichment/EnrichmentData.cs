using DigitalBoardGameList.App.Catalog.Input;
using DigitalBoardGameList.App.Catalog.Output;
using DigitalBoardGameList.App.Enrichment.Providers.Bgg;

namespace DigitalBoardGameList.App.Enrichment;

public class EnrichmentData
{
    public GameEntry Game { get; }
    public BggData Bgg { get; }
    public Dictionary<string, PlatformData> Platforms { get; } = new();

    public bool HasError { get; private set; }
    public void MarkError() => HasError = true;

    public int KnownTotalDlcCount { get; set; }

    public EnrichmentData(GameEntry game)
    {
        Game = game;
        Bgg = new BggData(game.BggId);

        foreach (var (platform, id) in Game.PlatformIds)
        {
            // Unknown platforms should provide direct url in place of id.
            var url = UrlHelper.GetUrlForKnownPlatform(platform, id) ?? id;
            Platforms.Add(platform, new PlatformData(url));
        }

        if (Game.Dlcs != null)
        {
            KnownTotalDlcCount = Game.Dlcs.Count;
        }
    }

    public GameDto ToDto()
    {
        return new GameDto(this);
    }
}