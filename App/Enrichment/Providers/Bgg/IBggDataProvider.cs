namespace DigitalBoardGameList.App.Enrichment.Providers.Bgg;

public interface IBggDataProvider
{
    public Task<string> LoadDataAsText();
}