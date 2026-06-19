using System.Collections.Frozen;

namespace DigitalBoardGameList.App.Enrichment;

public static class UrlHelper
{
    private static readonly FrozenDictionary<string, Func<string, string>> UrlMap = new Dictionary<string, Func<string, string>>
    {
        [Platform.Names.GooglePlay] = GooglePlay,
        [Platform.Names.AppStore] = AppStore,
        [Platform.Names.Steam] = Steam,
        [Platform.Names.GOG] = Gog,
        [Platform.Names.EGS] = Egs,
        [Platform.Names.Playstation] = Playstation,
        [Platform.Names.Xbox] = Xbox,
        [Platform.Names.Switch] = Switch,
    }.ToFrozenDictionary();

    public static string? GetUrlForKnownPlatform(string platform, string id)
    {
        return UrlMap.TryGetValue(platform, out var makeUrl) ? makeUrl(id) : null;
    }

    public static string Bgg(int id)
    {
        return $"https://boardgamegeek.com/boardgame/{id}";
    }

    public static string GooglePlay(string id)
    {
        return $"https://play.google.com/store/apps/details?id={id}";
    }

    public static string GooglePlayEnUs(string id)
    {
        return $"https://play.google.com/store/apps/details?id={id}&hl=en&gl=US";
    }

    public static string AppStore(string id)
    {
        return $"https://apps.apple.com/app/id{id}";
    }

    public static string ItunesApi(IEnumerable<string> ids)
    {
        return $"http://itunes.apple.com/lookup?id={string.Join(',', ids)}&country=us";
    }

    public static string Steam(string id)
    {
        return $"https://store.steampowered.com/app/{id}";
    }

    public static string SteamAppDetailsApi(string id)
    {
        return $"https://store.steampowered.com/api/appdetails?appids={id}&cc=us";
    }

    public static string SteamNewsApi(string id)
    {
        return $"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={id}";
    }

    public static string SteamDbPatchNotesRss(string id)
    {
        return $"https://steamdb.info/api/PatchnotesRSS/?appid={id}";
    }

    public static string Gog(string slug)
    {
        return $"https://www.gog.com/game/{slug}";
    }

    public static string GogPriceApi(IEnumerable<string> ids)
    {
        return $"https://api.gog.com/products/prices?ids={string.Join(',', ids)}&countryCode=US&currency=USD";
    }

    public static string Egs(string slug)
    {
        return $"https://store.epicgames.com/p/{slug}";
    }

    public static string Playstation(string id)
    {
        return $"https://store.playstation.com/en-us/product/{id}";
    }

    public static string Xbox(string id)
    {
        return $"https://www.xbox.com/games/store/{id}";
    }

    public static string Switch(string slug)
    {
        return $"https://www.nintendo.com/us/store/products/{slug}-switch";
    }
}