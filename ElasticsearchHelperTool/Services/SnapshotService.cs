using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Models.Snapshot.Restore;

namespace ElasticsearchHelperTool.Services;

public class SnapshotService
{
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly ElasticsearchSettings elasticsearchSettings;
    private readonly GetIndexDocumentCountService getIndexDocumentCountService;
    private readonly ReindexService reindexService;
    private readonly CreateIndexService createIndexService;
    private readonly DeleteIndexService deleteIndexService;

    public SnapshotService(
        ElasticsearchRestClient elasticsearchRestClient,
        ElasticsearchSettings elasticsearchSettings,
        GetIndexDocumentCountService getIndexDocumentCountService,
        ReindexService reindexService,
        CreateIndexService createIndexService,
        DeleteIndexService deleteIndexService)
    {
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.elasticsearchSettings = elasticsearchSettings;
        this.getIndexDocumentCountService = getIndexDocumentCountService;
        this.reindexService = reindexService;
        this.createIndexService = createIndexService;
        this.deleteIndexService = deleteIndexService;
    }

    public async Task<string> CreateSnapshotAsync(string indexName)
    {
        var snapshotName = $"helper-tool-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
        var response = await this.elasticsearchRestClient.CreateSnapshotAsync(indexName, snapshotName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to create snapshot {snapshotName}. Response: {response.StatusCode} {response.Content}");
        }

        Console.WriteLine($"Snapshot {snapshotName} created");
        return snapshotName;
    }

    public async Task<string> CreateIndexSnapshotAsync()
    {
        return await this.CreateSnapshotAsync(this.elasticsearchSettings.IndexV1Name);
    }

    public async Task<RestoreSnapshotResponse> RestoreSnapshotAsync(string indexName, string snapshotName)
    {
        var response = await this.elasticsearchRestClient.RestoreSnapshotAsync(indexName, snapshotName);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to restore snapshot {snapshotName} for index {indexName}. Response: {response.StatusCode} {response.Content}");
        }

        var restoreResponse = response.Data;

        if (restoreResponse is null)
        {
            throw new Exception("Failed to deserialize snapshot restore response");
        }

        Console.WriteLine($"Snapshot {snapshotName} for index {indexName} restored as {restoreResponse.Snapshot?.Indices?.First()}");
        return restoreResponse;
    }

    public async Task<RestoreSnapshotResponse> RestoreIndexV1SnapshotAsync(string snapshotName)
    {
        return await this.RestoreSnapshotAsync(this.elasticsearchSettings.IndexV1Name, snapshotName);
    }

    public async Task RestoreIndexV1SnapshotAndReindexAsync(string snapshotName)
    {
        var restoreSnapshotResponse = await this.RestoreIndexV1SnapshotAsync(snapshotName);
        var restoredIndexName = restoreSnapshotResponse.Snapshot?.Indices?.First();
        if (restoredIndexName is null)
        {
            throw new Exception("Failed to get restored index name");
        }

        var expectedDocumentCount = await this.getIndexDocumentCountService.GetDocumentCountFromIndexAsync(restoredIndexName);
        await this.deleteIndexService.DeleteIndexV1Async();
        await this.createIndexService.CreateIndexV1Async();
        await this.reindexService.ReindexAsync(restoredIndexName, this.elasticsearchSettings.IndexV1Name, expectedDocumentCount);
        await this.deleteIndexService.DeleteIndexAsync(restoredIndexName);
        Console.WriteLine("Done!");
    }
}
