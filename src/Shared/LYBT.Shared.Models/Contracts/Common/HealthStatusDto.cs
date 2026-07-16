using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Contracts.Common;

public class HealthStatusDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Healthy";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("database")]
    public string? Database { get; set; }

    [JsonPropertyName("dbResponseMs")]
    public long DbResponseMs { get; set; }

    [JsonPropertyName("statistics")]
    public HealthStatistics? Statistics { get; set; }
}

public class HealthStatistics
{
    [JsonPropertyName("totalUsers")]
    public int TotalUsers { get; set; }
}
