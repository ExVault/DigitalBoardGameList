namespace DigitalBoardGameList.App.Enrichment;

public class PlatformData
{
    public string Url { get; }
    public decimal? Price { get; set; }
    public DateOnly? LastUpdate { get; set; }

    public PlatformData(string url)
    {
        Url = url;
    }
}