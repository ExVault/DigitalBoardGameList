using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalBoardGameList.App;
using DigitalBoardGameList.App.Catalog.Input;
using DigitalBoardGameList.App.Catalog.Output;
using DigitalBoardGameList.App.Enrichment;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var stopwatch = Stopwatch.StartNew();

AppSettings settings;

try
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    settings = AppSettings.FromConfig(config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return;
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(settings.LogLevel)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level:u3}] {Message:l}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("AppSettings: {Settings}", settings);

var gameCatalog = GameCatalog.FromLocalYamlFile(settings.GameCatalogPath);
if (!gameCatalog.VerifyUnique())
{
    throw new InvalidOperationException("Game catalog is not unique");
}

Log.Information("Game catalog successfully loaded with {GameCount} games", gameCatalog.Games.Count);

var previousCatalog = PublishCatalog.FromLocalJsonFile(settings.PreviousCatalogPath);
if (previousCatalog == null)
{
    Log.Warning("No previous catalog loaded.");
}
else
{
    Log.Information("Previous catalog successfully loaded with {GameCount} games", previousCatalog.Games.Count);
}

var enrichment = new EnrichmentProcess(gameCatalog, previousCatalog, settings);

var publishCatalog = await enrichment.RunAsync();

var json = JsonSerializer.Serialize(publishCatalog,
    new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });

File.WriteAllText(settings.OutputCatalogPath, json);

Log.Information("Done! Time elapsed: {Elapsed}", PrettyPrintElapsed(stopwatch.Elapsed));

Console.WriteLine(enrichment.MakeFailureReport());

static string PrettyPrintElapsed(TimeSpan ts)
{
    if (ts.TotalMilliseconds < 1000)
        return $"{(int)ts.TotalMilliseconds}ms";

    if (ts.TotalSeconds < 60)
        return $"{ts.Seconds}s";

    if (ts.TotalMinutes < 60)
        return $"{ts.Minutes}m {ts.Seconds}s";

    return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
}