using System.Text.Json.Serialization;
using DigitalBoardGameList.App.Enrichment;
using DigitalBoardGameList.App.Enrichment.Providers.Bgg;
using JetBrains.Annotations;

namespace DigitalBoardGameList.App.Catalog.Output;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GameDto
{
    public string Name { get; init; } = null!;
    public BggData Bgg { get; init; } = null!;
    public List<PlatformUrlDto> Urls { get; init; } = [];
    public List<PlatformPriceDto> Prices { get; init; } = [];
    public List<PlatformLastUpdateDto> LastUpdates { get; init; } = [];
    public List<string>? Dlcs { get; init; }
    public string? ImageUrl { get; init; }
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public int KnownTotalDlcCount { get; init; }
    
    [JsonConstructor]
    public GameDto()
    {
    }
    
    public GameDto(EnrichmentData enrichedData)
    {
        Name = enrichedData.Game.Name;
        Bgg = enrichedData.Bgg;
        Dlcs = enrichedData.Game.Dlcs;
        ImageUrl = enrichedData.Game.ImageUrl;
        Developer = enrichedData.Game.Developer;
        Publisher = enrichedData.Game.Publisher;
        KnownTotalDlcCount = enrichedData.KnownTotalDlcCount;

        foreach (var (platform, data) in enrichedData.Platforms)
        {
            Urls.Add(new PlatformUrlDto(platform, data.Url));

            if (data.Price != null)
            {
                Prices.Add(new PlatformPriceDto(platform, data.Price));
            }
            if (data.LastUpdate != null)
            {
                LastUpdates.Add(new PlatformLastUpdateDto(platform, data.LastUpdate.GetValueOrDefault()));
            }
        }

        Urls.Sort((a, b) =>
        {
            var orderA = Platform.OrderMap.GetValueOrDefault(a.Platform, int.MaxValue);
            var orderB = Platform.OrderMap.GetValueOrDefault(b.Platform, int.MaxValue);
            return orderA.CompareTo(orderB);
        });

        Prices.Sort((a, b) => a.Price.Value.CompareTo(b.Price.Value));
        LastUpdates.Sort((a, b) => b.LastUpdate.CompareTo(a.LastUpdate));
    }


    public readonly record struct PlatformUrlDto(string Platform, string Url);

    public readonly record struct PlatformPriceDto(string Platform, PriceData Price);

    public readonly record struct PlatformLastUpdateDto(string Platform, DateOnly LastUpdate);
}