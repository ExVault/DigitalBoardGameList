using JetBrains.Annotations;

namespace DigitalBoardGameList.App.Catalog.Input;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GameEntry
{
    public required string Name { get; init; }
    public required int BggId { get; init; }
    public required Dictionary<string, string> PlatformIds { get; init; }
    public string? PriceTemplate { get; init; }

    public List<string>? Dlcs { get; set; }
    public string? ImageUrl { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
}