using System.Text.Json.Serialization;

namespace Bloxstrap.Models.BloxstrapRPC;

public class WallpaperMessage
{
    [JsonPropertyName("asset")]
    public string? Asset { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("reset")]
    public bool? Reset { get; set; }
}