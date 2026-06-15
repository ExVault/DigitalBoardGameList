namespace DigitalBoardGameList.App.Catalog.Output;

public class PublishCatalog : AbstractCatalog<GameDto>
{
    public PublishCatalog(IReadOnlyList<GameDto> games) : base(games)
    {
    }

    public static PublishCatalog? FromLocalJsonFile(string path)
    {
        if (!File.Exists(path))
            return null;
        
        return new PublishCatalog(CatalogLoader.FromLocalJsonFile<GameDto>(path));
    }
}