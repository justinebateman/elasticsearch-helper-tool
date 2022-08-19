using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;

namespace ElasticsearchHelperTool.Services;

public class DeleteIndexService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly ElasticsearchSettings elasticsearchSettings;

    public DeleteIndexService(ElasticsearchRestClient elasticsearchRestClient, ElasticsearchSettings elasticsearchSettings)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.elasticsearchSettings = elasticsearchSettings;
    }

    public async Task DeleteIndexAsync(string indexName)
    {
        var response = await this.elasticsearchRestClient.DeleteIndexAsync(indexName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to delete {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} deleted");
    }

    public async Task DeleteIndexV1Async()
    {
        await this.DeleteIndexAsync(this.elasticsearchSettings.IndexV1Name);
    }

    public async Task DeleteIndexV2Async()
    {
        await this.DeleteIndexAsync(this.elasticsearchSettings.IndexV2Name);
    }
}
