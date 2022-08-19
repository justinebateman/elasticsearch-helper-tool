using ElasticsearchHelperTool.Clients;
using Polly;

namespace ElasticsearchHelperTool.Services;

public class GetIndexDocumentCountService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;

    public GetIndexDocumentCountService(ElasticsearchRestClient elasticsearchRestClient)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
    }

    public async Task<int> GetDocumentCountFromIndexAsync(string indexName)
    {
        Console.WriteLine($"Getting document count from index {indexName}");
        var response = await this.elasticsearchRestClient.GetIndexDocumentCountAsync(indexName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to get document count from index {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        var countResponse = response.Data;
        if (countResponse is null)
        {
            throw new Exception($"Failed to deserialize document count response from index {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Document count from index {indexName}: {countResponse.Count}");
        return countResponse.Count;
    }

    public async Task RetryGetDocumentCountUntilCountMatchesExpectedAsync(string indexName, int expectedDocumentCount)
    {
        const int maxRetries = 10;
        var policy = Policy
            .Handle<Exception>()
            .OrResult<int>(r => r != expectedDocumentCount)
            .WaitAndRetryAsync(maxRetries, retryAttempt => TimeSpan.FromSeconds(1), onRetry: (exception, retryCount, context) =>
            {
                Console.WriteLine("Retrying get document count...");
            });
        var actualDocumentCount = await policy.ExecuteAsync(() => this.GetDocumentCountFromIndexAsync(indexName));
        if (actualDocumentCount != expectedDocumentCount)
        {
            throw new Exception($"Failed to get document count from index {indexName} after {maxRetries} retries. Expected: {expectedDocumentCount} Actual: {actualDocumentCount}");
        }
    }
}
