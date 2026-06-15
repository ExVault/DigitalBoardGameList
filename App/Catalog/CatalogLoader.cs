using System.Text.Json;
using YamlDotNet.Serialization;

namespace DigitalBoardGameList.App.Catalog;

public static class CatalogLoader
{
    public static List<T> FromLocalYamlFile<T>(string path)
    {
        return new Deserializer().Deserialize<List<T>>(File.ReadAllText(path));
    }

    public static List<T> FromLocalJsonFile<T>(string path)
    {
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path))!;
    }
}