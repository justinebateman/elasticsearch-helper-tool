using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using Newtonsoft.Json.Linq;

namespace ElasticsearchHelperTool.Services;

public class CreateIndexService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly ElasticsearchSettings elasticsearchSettings;

    public CreateIndexService(ElasticsearchRestClient elasticsearchRestClient, ElasticsearchSettings elasticsearchSettings)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.elasticsearchSettings = elasticsearchSettings;
    }

    public async Task CreateIndexAsync(string indexName, string mapping)
    {
        var response = await this.elasticsearchRestClient.CreateIndexAsync(indexName, mapping);

        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to create {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} created");
    }

    public async Task CreateIndexV1Async()
    {
        await this.CreateIndexAsync(this.elasticsearchSettings.IndexV1Name, this.GetIndexV1Mapping().ToString());
    }

    public async Task CreateIndexV2Async()
    {
        await this.CreateIndexAsync(this.elasticsearchSettings.IndexV2Name, this.GetIndexV2Mapping().ToString());
    }

    private JObject GetIndexV1Mapping()
    {
        // "../Mappings/index_mapping.json"
        return JObject.Parse(File.ReadAllText($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Mappings{Path.DirectorySeparatorChar}index_mapping.json"));
    }

    private JObject GetIndexV2Mapping()
    {
        var indexV2Mapping = JObject.FromObject(this.GetIndexV1Mapping());
        return this.UpdateIndexV2MappingAliasName(indexV2Mapping);
    }

    private JObject UpdateIndexV2MappingAliasName(JObject indexV2Mapping)
    {
        indexV2Mapping.SelectToken($"aliases.{this.elasticsearchSettings.IndexAlias}")?.Parent?.Remove();
        indexV2Mapping["aliases"]![$"{this.elasticsearchSettings.IndexAlias}2"] = new JObject();
        return indexV2Mapping;
    }
}
