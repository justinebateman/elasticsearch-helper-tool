namespace ElasticsearchHelperTool.Config
{
    public class ElasticsearchSettings
    {
        public bool UseLocal { get; set; }

        public string? Url { get; set; }

        public string? ApiKey { get; set; }

        public string IndexAlias { get; set; } = "things";
        
        public string IndexV1Name { get; set; }= "things-index-v1";

        public string IndexV2Name { get; set; }= "things-index-v2";

        public string SnapshotRepositoryName { get; set; } = "found-snapshots";

        
    }
}
