using Newtonsoft.Json;

namespace ElasticsearchHelperTool.Models.Reindex;

public class ReindexResponse
{
    [JsonProperty("took")]
    public int Took { get; set; }
    [JsonProperty("timed_out")]
    public bool TimedOut { get; set; }
    [JsonProperty("total")]
    public int Total { get; set; }
    [JsonProperty("updated")]
    public int Updated { get; set; }
    [JsonProperty("created")]
    public int Created { get; set; }
    [JsonProperty("deleted")]
    public int Deleted { get; set; }
    [JsonProperty("batches")]
    public int Batches { get; set; }
    [JsonProperty("version_conflicts")]
    public int VersionConflicts { get; set; }
    [JsonProperty("noops")]
    public int Noops { get; set; }
    [JsonProperty("throttled_millis")]
    public int ThrottledMillis { get; set; }
    [JsonProperty("requests_per_second")]
    public double RequestsPerSecond { get; set; }
    [JsonProperty("throttled_until_millis")]
    public int ThrottledUntilMillis { get; set; }
    [JsonProperty("failures")]
    public List<object>? Failures { get; set; }
}
