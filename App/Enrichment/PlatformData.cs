namespace DigitalBoardGameList.App.Enrichment;

public class PlatformData
{
    public string Url { get; }
    public PriceData? Price { get; set; }
    public DateOnly? LastUpdate { get; set; }

    public PlatformData(string url)
    {
        Url = url;
    }
}