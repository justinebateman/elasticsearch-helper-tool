using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Models.Reindex;
using ElasticsearchHelperTool.Models.Snapshot;
using Newtonsoft.Json;
using RestSharp;

namespace ElasticsearchHelperTool.Clients;

public class ElasticsearchRestClient
{
    private readonly ElasticsearchSettings elasticsearchSettings;
    private readonly RestClient client;

    public ElasticsearchRestClient(ElasticsearchSettings elasticsearchSettings)
    {
        this.elasticsearchSettings = elasticsearchSettings;
        if (String.IsNullOrEmpty(elasticsearchSettings?.Url))
        {
            throw new Exception("ElasticsearchUrl is not set");
        }

        var options = new RestClientOptions(elasticsearchSettings.Url)
        {
            ThrowOnAnyError = false, MaxTimeout = 30000,
        };
        this.client = new RestClient(options)
            .AddDefaultHeader("Authorization", $"ApiKey {elasticsearchSettings.ApiKey}");
    }

    public Task<RestResponse> GetIndexMapping(string indexName)
    {
        var request = new RestRequest($"/{indexName}/_mapping", Method.Get);
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> CreateSnapshot(string indexName, string snapshotName)
    {
        var request = new RestRequest($"/_snapshot/{this.elasticsearchSettings.SnapshotRepositoryName}/{snapshotName}", Method.Put)
            .AddJsonBody(new CreateSnapshotRequest()
            {
                Indices = indexName,
                Metadata = new SnapshotMetadata()
                {
                    TakenBy = "Elastic Helper Tool", TakenBecause = "Backup before performing action in Elastic Helper Tool"
                }
            });
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> RestoreSnapshot(string indexName, string snapshotName)
    {
        var request = new RestRequest($"/_snapshot/{this.elasticsearchSettings.SnapshotRepositoryName}/{snapshotName}/_restore", Method.Post)
            .AddJsonBody(new RestoreSnapshotRequest()
            {
                Indices = indexName,
            });
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> CreateIndex(string indexName, string mapping)
    {
        var request = new RestRequest($"/{indexName}", Method.Put)
            .AddJsonBody(mapping);
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> ReIndex(string sourceIndexName, string destinationIndexName)
    {
        ReindexRequest reindexRequest = new ReindexRequest()
        {
            Source = new ReindexSource()
            {
                Index = sourceIndexName
            },
            Dest = new ReindexDestination()
            {
                Index = destinationIndexName
            },
        };

        var request = new RestRequest("/_reindex", Method.Post)
            .AddJsonBody(JsonConvert.SerializeObject(reindexRequest));
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> GetIndexDocumentCount(string indexName)
    {
        var request = new RestRequest($"/{indexName}/_count", Method.Get);
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse> DeleteIndex(string indexName)
    {
        var request = new RestRequest($"/{indexName}", Method.Delete);
        return this.client.ExecuteAsync(request);
    }

    // useful for debugging - add this.LogRequest(request); before this.client.ExecuteAsync(request);
    private void LogRequest(RestRequest request)
    {
        var requestToLog = new
        {
            resource = request.Resource,
            parameters = request.Parameters.Select(parameter => new
            {
                name = parameter.Name,
                value = parameter.Value,
                type = parameter.Type.ToString()
            }),
            method = request.Method.ToString(),
            defaultParameters = this.client.DefaultParameters,
            // This will generate the actual Uri used in the request
            uri = this.client.BuildUri(request),
        };
        Console.WriteLine($"**Request**: \n{JsonConvert.SerializeObject(requestToLog)}");
    }
}
