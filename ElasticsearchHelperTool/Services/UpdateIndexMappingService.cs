using ElasticsearchHelperTool.Config;

namespace ElasticsearchHelperTool.Services;

public class UpdateIndexMappingService
{
    private readonly ElasticsearchSettings elasticsearchSettings;
    private readonly GetIndexDocumentCountService getIndexDocumentCountService;
    private readonly CreateIndexService createIndexService;
    private readonly DeleteIndexService deleteIndexService;
    private readonly SnapshotService snapshotService;
    private readonly ReindexService reindexService;

    public UpdateIndexMappingService(
        ElasticsearchSettings elasticsearchSettings,
        GetIndexDocumentCountService getIndexDocumentCountService,
        CreateIndexService createIndexService,
        DeleteIndexService deleteIndexService,
        SnapshotService snapshotService,
        ReindexService reindexService)
    {
        this.elasticsearchSettings = elasticsearchSettings;
        this.getIndexDocumentCountService = getIndexDocumentCountService;
        this.createIndexService = createIndexService;
        this.deleteIndexService = deleteIndexService;
        this.snapshotService = snapshotService;
        this.reindexService = reindexService;
    }

    public async Task UpdateIndexMappingAsync()
    {
        string indexV1Name = this.elasticsearchSettings.IndexV1Name;
        string indexV2Name = this.elasticsearchSettings.IndexV2Name;

        // create a snapshot before performing any action
        if (!this.elasticsearchSettings.UseLocal)
        {
            await this.snapshotService.CreateSnapshotAsync(indexV1Name);
        }

        // get existing document count
        var existingDocumentCount = await this.getIndexDocumentCountService.GetDocumentCountFromIndexAsync(indexV1Name);

        await this.createIndexService.CreateIndexV2Async();

        await this.reindexService.ReindexAsync(indexV1Name, indexV2Name, existingDocumentCount);

        await this.deleteIndexService.DeleteIndexV1Async();

        await this.createIndexService.CreateIndexV1Async();

        await this.reindexService.ReindexAsync(indexV2Name, indexV1Name, existingDocumentCount);

        await this.deleteIndexService.DeleteIndexV2Async();

        Console.WriteLine("Done!");
    }
}
