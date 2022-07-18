using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Snapshot;

public class SnapshotMetadata
{
    [JsonProperty("taken_by")]
    public string? TakenBy { get; set; }
    [JsonProperty("taken_because")]
    public string? TakenBecause { get; set; }
}
