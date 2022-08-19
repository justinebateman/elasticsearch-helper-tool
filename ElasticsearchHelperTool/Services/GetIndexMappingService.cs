using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using Newtonsoft.Json.Linq;

namespace ElasticsearchHelperTool.Services;

public class GetIndexMappingService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly ElasticsearchSettings elasticsearchSettings;

    public GetIndexMappingService(ElasticsearchRestClient elasticsearchRestClient, ElasticsearchSettings elasticsearchSettings)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.elasticsearchSettings = elasticsearchSettings;
    }

    public async Task<JObject> GetIndexMappingAsync(string indexName, bool printMappingToLogs = true)
    {
        var response = await this.elasticsearchRestClient.GetIndexMappingAsync(indexName);

        if (!response.IsSuccessful || response.Data is null)
        {
            throw new Exception($"Failed to get index {indexName} mapping. Response: {response.StatusCode} {response.Content}");
        }

        if (printMappingToLogs)
            Console.WriteLine($"Get index mapping response: {response.StatusCode} {response.Data}");

        return response.Data;
    }

    public async Task<JObject> GetIndexV1MappingAsync(bool printMappingToLogs = true)
    {
        return await this.GetIndexMappingAsync(this.elasticsearchSettings.IndexV1Name, printMappingToLogs);
    }
}
