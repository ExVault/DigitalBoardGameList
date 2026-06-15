using System.Collections.Frozen;
using System.Reflection;

namespace DigitalBoardGameList.App;

public static class Platform
{
    public static class Names
    {
        public const string GooglePlay = nameof(GooglePlay);
        public const string AppStore = nameof(AppStore);
        public const string Steam = nameof(Steam);
        public const string GOG = nameof(GOG);
        public const string EGS = nameof(EGS);
        public const string Playstation = nameof(Playstation);
        public const string Xbox = nameof(Xbox);
        public const string Switch = nameof(Switch);
    }

    public static IReadOnlyList<string> List { get; }
    public static FrozenDictionary<string, int> OrderMap { get; }

    static Platform()
    {
        List = typeof(Names)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        var orderMap = new Dictionary<string, int>
        {
            [Names.GooglePlay] = 1,
            [Names.AppStore] = 2,
            [Names.Steam] = 3,
            [Names.GOG] = 4,
            [Names.EGS] = 5,
            [Names.Playstation] = 6,
            [Names.Xbox] = 7,
            [Names.Switch] = 8,
        };

        if (orderMap.Count != List.Count)
        {
            throw new InvalidOperationException("OrderMap does not contain all platforms");
        }

        OrderMap = orderMap.ToFrozenDictionary();
    }
}