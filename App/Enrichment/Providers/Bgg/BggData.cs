namespace DigitalBoardGameList.App.Enrichment.Providers.Bgg;

public class BggData
{
    public int Id { get; }
    public string Url { get; }

    public int Rank { get; set; }
    public double Rating { get; set; }

    public BggData(int id)
    {
        Id = id;
        Url = UrlHelper.Bgg(id);
    }
}