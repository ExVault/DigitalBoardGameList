using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalBoardGameList.App.Catalog.Output;

public class PublishCatalog
{
    public IReadOnlyList<GameDto> Games { get; init; } = null!;
    public DateTimeOffset PublishDate { get; init; }

    public PublishCatalog(List<GameDto> games)
    {
        games.Sort((a, b) => a.Bgg.Rank.CompareTo(b.Bgg.Rank));
        Games = games;
        PublishDate = DateTimeOffset.UtcNow;
    }

    public static PublishCatalog? FromLocalJsonFile(string? path)
    {
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<PublishCatalog>(File.ReadAllText(path))!;
    }

    [JsonConstructor]
    public PublishCatalog()
    {
    }
}