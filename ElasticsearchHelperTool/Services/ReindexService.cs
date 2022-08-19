using ElasticsearchHelperTool.Clients;

namespace ElasticsearchHelperTool.Services;

public class ReindexService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly GetIndexDocumentCountService getIndexDocumentCountService;

    public ReindexService(ElasticsearchRestClient elasticsearchRestClient, GetIndexDocumentCountService getIndexDocumentCountService)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.getIndexDocumentCountService = getIndexDocumentCountService;
    }

    public async Task ReindexAsync(string sourceIndexName, string destinationIndexName, int expectedDocumentCount)
    {
        Console.WriteLine($"Starting Reindex from {sourceIndexName} to {destinationIndexName}. Expected documents: {expectedDocumentCount}");
        var response = await this.elasticsearchRestClient.ReIndexAsync(sourceIndexName, destinationIndexName);

        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexName}. Response: {response.StatusCode} {response.Content}");
        }

        var reindexResponse = response.Data;
        if (reindexResponse is null)
        {
            throw new Exception($"Failed to deserialize reindex response. Response: {response.StatusCode} {response.Content}");
        }

        if (reindexResponse.Total != expectedDocumentCount)
        {
            throw new Exception($"Failed to reindex from {sourceIndexName} to {destinationIndexName}. Expected document count: {expectedDocumentCount} Actual document count: {reindexResponse.Total}");
        }

        await this.getIndexDocumentCountService.RetryGetDocumentCountUntilCountMatchesExpectedAsync(destinationIndexName, expectedDocumentCount);

        Console.WriteLine($"Reindex from {sourceIndexName} to {destinationIndexName} completed. Total documents: {reindexResponse.Total}");
    }
}
