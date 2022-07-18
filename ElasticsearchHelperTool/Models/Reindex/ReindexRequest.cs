using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Reindex;

public class ReindexRequest
{
    [JsonProperty("source")]
    public ReindexSource? Source;
    [JsonProperty("dest")]
    public ReindexDestination? Dest;
}
