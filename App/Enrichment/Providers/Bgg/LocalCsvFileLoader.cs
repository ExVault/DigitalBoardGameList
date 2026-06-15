namespace DigitalBoardGameList.App.Enrichment.Providers.Bgg;

public class LocalCsvFileLoader : IBggDataProvider
{
    private readonly string _path;

    public LocalCsvFileLoader(string path)
    {
        _path = path;
    }

    public async Task<string> LoadDataAsText()
    {
        return await File.ReadAllTextAsync(_path);
    }
}