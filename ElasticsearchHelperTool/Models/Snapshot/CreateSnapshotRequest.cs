using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Snapshot;

public class CreateSnapshotRequest
{
    [JsonProperty("indices")]
    public string? Indices { get; set; }
    [JsonProperty("metadata")]
    public SnapshotMetadata? Metadata { get; set; }
}
