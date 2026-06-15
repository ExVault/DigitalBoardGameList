namespace DigitalBoardGameList.App.Network;

public static class SharedHttpClient
{
    public static readonly HttpClient Instance = ConfigureHttpClient();

    private static HttpClient ConfigureHttpClient()
    {
        var httpClient = new HttpClient();

        httpClient.Timeout = TimeSpan.FromSeconds(45);

        var headers = httpClient.DefaultRequestHeaders;

        headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:150.0) Gecko/20100101 Firefox/150.0");
        //headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        headers.Accept.ParseAdd("*/*");
        headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        headers.Connection.ParseAdd("keep-alive");

        // headers.AcceptEncoding.ParseAdd("gzip, deflate, br, zstd");

        // headers.Add("Upgrade-Insecure-Requests", "1");
        // headers.Add("Sec-Fetch-Dest", "document");
        // headers.Add("Sec-Fetch-Mode", "navigate");
        // headers.Add("Sec-Fetch-Site", "none");
        // headers.Add("Sec-Fetch-User", "?1");

        return httpClient;
    }
}