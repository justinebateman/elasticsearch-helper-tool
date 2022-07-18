using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.DocumentCount;

public class ShardsResponse
{
    [JsonProperty("total")]
    public int Total { get; set; }
    [JsonProperty("successful")]
    public int Successful { get; set; }
    [JsonProperty("skipped")]
    public int Skipped { get; set; }
    [JsonProperty("failed")]
    public int Failed { get; set; }
}
