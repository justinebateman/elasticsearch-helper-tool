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

    public async Task UpdateIndexMappingAsync()
    {
        string indexV1Name = this.elasticsearchSettings.IndexV1Name;
        string indexV2Name = this.elasticsearchSettings.IndexV2Name;

        // create a snapshot before performing any action
        if (!this.elasticsearchSettings.UseLocal)
        {
            await this.CreateSnapshotAsync(indexV1Name);
        }

        // get existing document count
        var existingDocumentCount = await this.GetDocumentCountFromIndexAsync(indexV1Name);

        // "../Mappings/index_mapping.json"
        var indexV1Mapping = JObject.Parse(File.ReadAllText($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}mappings{Path.DirectorySeparatorChar}index_mapping.json"));
        var indexV2Mapping = JObject.FromObject(indexV1Mapping);
        indexV2Mapping = this.UpdateIndexV2MappingAliasNameAsync(indexV2Mapping);

        await this.CreateIndexAsync(indexV2Name, indexV2Mapping.ToString());

        await this.ReindexAsync(indexV1Name, indexV2Name, existingDocumentCount);

        await this.DeleteIndexAsync(indexV1Name);

        await this.CreateIndexAsync(indexV1Name, indexV1Mapping.ToString());

        await this.ReindexAsync(indexV2Name, indexV1Name, existingDocumentCount);

        await this.DeleteIndexAsync(indexV2Name);

        Console.WriteLine("Done!");
    }

    private async Task<string> CreateSnapshotAsync(string indexName)
    {
        var snapshotName = $"helper-tool-{DateTime.Now.ToString("yyyyMMddHHmmsss")}";
        var response = await this.elasticsearchRestClient.CreateSnapshotAsync(indexName, snapshotName);
        if (response.IsSuccessful)
        {
            Console.WriteLine($"Snapshot {snapshotName} created");
        }
        else
        {
            throw new Exception($"Failed to create snapshot {snapshotName}. Response: {response.StatusCode} {response.Content}");
        }

        return snapshotName;
    }

    private JObject UpdateIndexV2MappingAliasNameAsync(JObject indexV2Mapping)
    {
        indexV2Mapping.SelectToken($"aliases.{elasticsearchSettings.IndexAlias}")?.Parent?.Remove();
        indexV2Mapping["aliases"]![$"{elasticsearchSettings.IndexAlias}2"] = new JObject();
        return indexV2Mapping;
    }

    private async Task CreateIndexAsync(string indexName, string mapping)
    {
        var response = await this.elasticsearchRestClient.CreateIndexAsync(indexName, mapping);

        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to create {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} created");
    }

    private async Task<int> GetDocumentCountFromIndexAsync(string indexName)
    {
        Console.WriteLine($"Getting document count from index {indexName}");
        var response = await this.elasticsearchRestClient.GetIndexDocumentCountAsync(indexName);
        if (response.IsSuccessful && response.Content != null)
        {
            var countResponse = JsonConvert.DeserializeObject<CountResponse>(response.Content);
            if (countResponse != null)
            {
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

    private async Task RetryGetDocumentCountUntilCountMatchesExpectedAsync(string indexName, int expectedDocumentCount)
    {
        var policy = Policy
            .Handle<Exception>()
            .OrResult<int>(r => r != expectedDocumentCount)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1), onRetry: (exception, retryCount, context) =>
            {
                Console.WriteLine("Retrying get document count...");
            });
        var actualDocumentCount = await policy.ExecuteAsync(() => this.GetDocumentCountFromIndexAsync(indexName));
        if (actualDocumentCount != expectedDocumentCount)
        {
            throw new Exception($"Failed to get document count from index {indexName} after 3 retries. Expected: {expectedDocumentCount} Actual: {actualDocumentCount}");
        }
    }

    private async Task ReindexAsync(string sourceIndexName, string destinationIndexName, int expectedDocumentCount)
    {
        Console.WriteLine($"Starting Reindex from {sourceIndexName} to {destinationIndexName}. Expected documents: {expectedDocumentCount}");
        var response = await this.elasticsearchRestClient.ReIndex(sourceIndexName, destinationIndexName);

        if (!response.IsSuccessful || response.Content == null)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexName}. Response: {response.StatusCode} {response.Content ?? "No content"}");
        }

        var reindexResponse = JsonConvert.DeserializeObject<ReindexResponse>(response.Content);
        if (reindexResponse?.Total == null || reindexResponse.Total != expectedDocumentCount)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexName}. Expected document count: {expectedDocumentCount} Actual document count: {reindexResponse?.Total}");
        }

        await this.RetryGetDocumentCountUntilCountMatchesExpectedAsync(destinationIndexName, expectedDocumentCount);

        Console.WriteLine($"Reindex from {sourceIndexName} to {destinationIndexName} completed. Total documents: {reindexResponse?.Total}");
    }

    private async Task DeleteIndexAsync(string indexName)
    {
        var response = await this.elasticsearchRestClient.DeleteIndexAsync(indexName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to delete {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Index {indexName} deleted");
    }
}
