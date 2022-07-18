using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Reindex;

public class ReindexSource
{
    [JsonProperty("index")]
    public string? Index { get; set; }
}
