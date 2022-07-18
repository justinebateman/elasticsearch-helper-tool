using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Snapshot;

public class RestoreSnapshotRequest
{
    [JsonProperty("indices")]
    public string? Indices { get; set; }

    [JsonProperty("rename_pattern")]
    public string RenamePattern { get; set; } = "(.+)";

    [JsonProperty("rename_replacement")]
    public string RenameReplacement { get; set; } = "restored-$1";
}
