namespace ElasticsearchHelperTool.Models.Snapshot.Restore;

public class Shards
{
    public int Total { get; set; }

    public int Failed { get; set; }

    public int Successful { get; set; }
}
