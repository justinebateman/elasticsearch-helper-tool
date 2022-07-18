using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.DocumentCount;

public class CountResponse
{
    [JsonProperty("count")]
    public int Count { get; set; }
    [JsonProperty("_shards")]
    public ShardsResponse? Shards { get; set; }
}
