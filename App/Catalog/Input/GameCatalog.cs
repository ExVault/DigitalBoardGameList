using Serilog;
using YamlDotNet.Serialization;

namespace DigitalBoardGameList.App.Catalog.Input;

public class GameCatalog
{
    public IReadOnlyList<GameEntry> Games { get; }

    private GameCatalog(IReadOnlyList<GameEntry> games)
    {
        Games = games;
    }

    public static GameCatalog FromLocalYamlFile(string path)
    {
        var list = new Deserializer().Deserialize<List<GameEntry>>(File.ReadAllText(path));
        return new GameCatalog(list);
    }

    public bool VerifyUnique()
    {
        var seenNames = new HashSet<string>(Games.Count, StringComparer.OrdinalIgnoreCase);
        var seenBggIds = new HashSet<int>(Games.Count);
        var seenPlatformIds = new Dictionary<string, HashSet<string>>(Platform.List.Count);

        foreach (var platform in Platform.List)
        {
            seenPlatformIds.Add(platform, new HashSet<string>());
        }

        bool result = true;

        foreach (var game in Games)
        {
            if (!seenNames.Add(game.Name))
            {
                Log.Error("Duplicate game name - {Name}", game.Name);
                result = false;
            }
            if (!seenBggIds.Add(game.BggId))
            {
                Log.Error("Duplicate BGG id - {BggId}", game.BggId);
                result = false;
            }

            foreach (var (platform, id) in game.PlatformIds)
            {
                if (seenPlatformIds.TryGetValue(platform, out var hashSet))
                {
                    if (!hashSet.Add(id))
                    {
                        Log.Error("Duplicate id on {Platform} - {Id}", platform, id);
                        result = false;
                    }
                }
            }
        }
        return result;
    }
}