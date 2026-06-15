using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace DigitalBoardGameList.App;

public class AppSettings
{
    public LogEventLevel LogLevel { get; }

    public string GameCatalogPath { get; }
    public string OutputCatalogPath { get; }
    public string? PreviousCatalogPath { get; }
    public string BggCsvPath { get; }

    public bool ProcessOnlyNewGames { get; }
    public bool ForceDlcUpdate { get; }
    public bool TestRun { get; }

    private AppSettings(LogEventLevel logLevel, string gameCatalogPath, string outputCatalogPath, string? previousCatalogPath,
        string bggCsvPath, bool processOnlyNewGames, bool forceDlcUpdate, bool testRun)
    {
        LogLevel = logLevel;
        GameCatalogPath = gameCatalogPath;
        OutputCatalogPath = outputCatalogPath;
        BggCsvPath = bggCsvPath;
        ProcessOnlyNewGames = processOnlyNewGames;
        ForceDlcUpdate = forceDlcUpdate;
        TestRun = testRun;

        if (string.IsNullOrEmpty(previousCatalogPath))
        {
            PreviousCatalogPath = null;
        }
        else if (previousCatalogPath.Equals(nameof(OutputCatalogPath), StringComparison.OrdinalIgnoreCase))
        {
            PreviousCatalogPath = outputCatalogPath;
        }
        else
        {
            PreviousCatalogPath = previousCatalogPath;
        }
    }

    public static AppSettings FromConfig(IConfigurationRoot config)
    {
        return new AppSettings(
            logLevel: Enum.Parse<LogEventLevel>(GetRequiredString(nameof(LogLevel), config), ignoreCase: true),
            gameCatalogPath: GetRequiredString(nameof(GameCatalogPath), config),
            outputCatalogPath: GetRequiredString(nameof(OutputCatalogPath), config),
            previousCatalogPath: config[nameof(PreviousCatalogPath)],
            bggCsvPath: GetRequiredString(nameof(BggCsvPath), config),
            processOnlyNewGames: GetBoolOrDefault("OnlyNew", config),
            forceDlcUpdate: GetBoolOrDefault(nameof(ForceDlcUpdate), config),
            testRun: GetBoolOrDefault(nameof(TestRun), config)
        );
    }

    private static string GetRequiredString(string key, IConfigurationRoot config)
    {
        return config[key] ?? throw new InvalidOperationException($"Required '{key}' config is missing");
    }

    private static bool GetBoolOrDefault(string key, IConfigurationRoot config)
    {
        var strValue = config[key];
        return strValue != null && bool.Parse(strValue);
    }

    public override string ToString()
    {
        return string.Join(", ", GetType().GetProperties().Select(p => $"{p.Name} = {p.GetValue(this)}"));
    }
}