namespace ElasticsearchHelperTool.Models.Snapshot.Restore;

public class SnapshotResponse
{
    public string? Snapshot { get; set; }

    public List<string>? Indices { get; set; }

    public Shards? Shards { get; set; }
}
