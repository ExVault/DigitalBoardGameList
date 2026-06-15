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

var inputCatalog = GameCatalog.FromLocalYamlFile(settings.InputPath);
if (!inputCatalog.VerifyUnique())
{
    throw new InvalidOperationException("Game input catalogue is not unique");
}

Log.Information("Input catalog successfully loaded with {GameCount} games", inputCatalog.Games.Count);

var previousCatalog = PublishCatalog.FromLocalJsonFile(settings.OutputPath);

Log.Information("Previous catalog successfully loaded with {GameCount} games", previousCatalog.Games.Count);

var enrichment = new EnrichmentProcess(inputCatalog, previousCatalog, settings);

var publishCatalog = await enrichment.RunAsync();

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var json = JsonSerializer.Serialize(publishCatalog.Games.OrderBy(d => d.Bgg.Rank), jsonOptions);

var outputPath = settings.TestRun
    ? Path.Combine(Path.GetDirectoryName(settings.OutputPath)!, "test_" + Path.GetFileName(settings.OutputPath))
    : settings.OutputPath;

File.WriteAllText(outputPath, json);

Console.WriteLine("Done!");