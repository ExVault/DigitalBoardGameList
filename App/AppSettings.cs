using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace DigitalBoardGameList.App;

public class AppSettings
{
    public LogEventLevel LogLevel { get; }

    public string InputPath { get; }
    public string OutputPath { get; }
    public string BggCsvPath { get; }

    public bool ProcessOnlyNewGames { get; }
    public bool ForceDlcUpdate { get; }
    public bool TestRun { get; }

    private AppSettings(LogEventLevel logLevel, string inputPath, string outputPath, string bggCsvPath,
        bool processOnlyNewGames, bool forceDlcUpdate, bool testRun)
    {
        LogLevel = logLevel;
        InputPath = inputPath;
        OutputPath = outputPath;
        BggCsvPath = bggCsvPath;
        ProcessOnlyNewGames = processOnlyNewGames;
        ForceDlcUpdate = forceDlcUpdate;
        TestRun = testRun;
    }

    public static AppSettings FromConfig(IConfigurationRoot config)
    {
        return new AppSettings(
            logLevel: Enum.Parse<LogEventLevel>(GetRequiredString(nameof(LogLevel), config), ignoreCase: true),
            inputPath: GetRequiredString(nameof(InputPath), config),
            outputPath: GetRequiredString(nameof(OutputPath), config),
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