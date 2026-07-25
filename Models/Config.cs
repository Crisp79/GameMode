using System.Text.Json.Serialization;

namespace GameMode.Models;

public class Config
{
    [JsonPropertyName("PlaynitePath")]
    public string PlaynitePath { get; set; } = string.Empty;

    [JsonPropertyName("CheckIntervalMs")]
    public int CheckIntervalMs { get; set; } = 500;

    [JsonPropertyName("DisconnectTimeoutMinutes")]
    public int DisconnectTimeoutMinutes { get; set; } = 5;

    [JsonPropertyName("ClosePlaynite")]
    public bool ClosePlaynite { get; set; } = true;

    [JsonPropertyName("BringToFront")]
    public bool BringToFront { get; set; } = true;

    [JsonPropertyName("HideCursor")]
    public bool HideCursor { get; set; } = false;
}
