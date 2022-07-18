using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Reindex;

public class ReindexDestination
{
    [JsonProperty("index")]
    public string? Index { get; set; }
}
