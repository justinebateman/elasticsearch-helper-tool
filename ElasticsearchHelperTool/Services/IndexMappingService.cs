using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Models.DocumentCount;
using ElasticsearchHelperTool.Models.Reindex;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace ElasticsearchHelperTool.Services;

public class IndexMappingService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly ElasticsearchSettings elasticsearchSettings;

    public IndexMappingService(ElasticsearchRestClient elasticsearchRestClient, ElasticsearchSettings elasticsearchSettings)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.elasticsearchSettings = elasticsearchSettings;
    }

    public async Task UpdateIndexMapping()
    {
        string indexV1Name = this.elasticsearchSettings.IndexV1Name;
        string indexV2Name = this.elasticsearchSettings.IndexV2Name;

        string snapshotName = "";

        // create a snapshot before performing any action
        if (!this.elasticsearchSettings.UseLocal)
        {
            snapshotName = $"helper-tool-{DateTime.Now.ToString("YYYYMMDDHHmmsss")}";
            await this.elasticsearchRestClient.CreateSnapshot(indexV1Name, snapshotName);
        }

        // get existing document count
        var existingDocumentCount = await this.GetDocumentCountFromIndex(indexV1Name);

        // "../Mappings/index_mapping.json"
        var indexV1Mapping = JObject.Parse(File.ReadAllText($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}mappings{Path.DirectorySeparatorChar}index_mapping.json"));
        var indexV2Mapping = JObject.FromObject(indexV1Mapping);
        indexV2Mapping = this.UpdateIndexV2MappingAliasName(indexV2Mapping);

        await this.CreateIndex(indexV2Name, indexV2Mapping.ToString());

        await this.Reindex(indexV1Name, indexV2Name, existingDocumentCount);

        await this.DeleteIndex(indexV1Name);

        await this.CreateIndex(indexV1Name, indexV1Mapping.ToString());

        await this.Reindex(indexV2Name, indexV1Name, existingDocumentCount);

        await this.DeleteIndex(indexV2Name);

        Console.WriteLine("Done!");
    }

    private JObject UpdateIndexV2MappingAliasName(JObject indexV2Mapping)
    {
        indexV2Mapping.SelectToken($"aliases.{elasticsearchSettings.IndexAlias}")?.Parent?.Remove();
        indexV2Mapping["aliases"]![$"{elasticsearchSettings.IndexAlias}2"] = new JObject();
        return indexV2Mapping;
    }

    private async Task CreateIndex(string indexName, string mapping)
    {
        var response = await this.elasticsearchRestClient.CreateIndex(indexName, mapping);

        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to create {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} created");
    }

    private async Task<int> GetDocumentCountFromIndex(string indexName)
    {
        Console.WriteLine($"Getting document count from index {indexName}");
        var response = await this.elasticsearchRestClient.GetIndexDocumentCount(indexName);
        if (response.IsSuccessful && response.Content != null)
        {
            var countResponse = JsonConvert.DeserializeObject<CountResponse>(response.Content);
            if (countResponse != null)
            {
                // TODO remove
                Console.WriteLine($"Document count from index {indexName}: {countResponse.Count}");
                return countResponse.Count;
            }
            else
            {
                throw new Exception($"Failed to deserialize document count response from index {indexName}. Response: {response.StatusCode} {response.Content}");
            }
        }
        else
        {
            throw new Exception($"Failed to get document count from index {indexName}. Response: {response.StatusCode} {response.Content}");
        }
    }

    private async Task RetryGetDocumentCountUntilCountMatchesExpected(string indexName, int expectedDocumentCount)
    {
        var policy = Policy
            .Handle<Exception>()
            .OrResult<int>(r => r != expectedDocumentCount)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1), onRetry: (exception, retryCount, context) =>
            {
                Console.WriteLine("Retrying get document count...");
            });
        var actualDocumentCount = await policy.ExecuteAsync(() => this.GetDocumentCountFromIndex(indexName));
        if (actualDocumentCount != expectedDocumentCount)
        {
            throw new Exception($"Failed to get document count from index {indexName} after 3 retries. Expected: {expectedDocumentCount} Actual: {actualDocumentCount}");
        }
    }

    private async Task Reindex(string sourceIndexName, string destinationIndexNameTwo, int expectedDocumentCount)
    {
        Console.WriteLine($"Starting Reindex from {sourceIndexName} to {destinationIndexNameTwo}. Expected documents: {expectedDocumentCount}");
        var response = await this.elasticsearchRestClient.ReIndex(sourceIndexName, destinationIndexNameTwo);

        if (!response.IsSuccessful || response.Content == null)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexNameTwo}. Response: {response.StatusCode} {response.Content ?? "No content"}");
        }

        var reindexResponse = JsonConvert.DeserializeObject<ReindexResponse>(response.Content);
        if (reindexResponse?.Total == null || reindexResponse.Total != expectedDocumentCount)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexNameTwo}. Expected document count: {expectedDocumentCount} Actual document count: {reindexResponse?.Total}");
        }

        await this.RetryGetDocumentCountUntilCountMatchesExpected(destinationIndexNameTwo, expectedDocumentCount);

        Console.WriteLine($"Reindex from {sourceIndexName} to {destinationIndexNameTwo} completed. Total documents: {reindexResponse?.Total}");
    }

    private async Task DeleteIndex(string indexName)
    {
        var response = await this.elasticsearchRestClient.DeleteIndex(indexName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to delete {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} deleted");
    }
}
