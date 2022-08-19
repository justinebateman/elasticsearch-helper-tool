using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Models.DocumentCount;
using ElasticsearchHelperTool.Models.Reindex;
using ElasticsearchHelperTool.Models.Snapshot;
using ElasticsearchHelperTool.Models.Snapshot.Restore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ElasticsearchHelperTool.Clients;

public class ElasticsearchRestClient
{
    private readonly ElasticsearchSettings elasticsearchSettings;
    private readonly JsonSerializerSettings jsonSerializerSettings;
    private readonly RestClient client;

    public ElasticsearchRestClient(ElasticsearchSettings elasticsearchSettings, JsonSerializerSettings jsonSerializerSettings, RestClient client)
    {
        this.elasticsearchSettings = elasticsearchSettings;
        this.jsonSerializerSettings = jsonSerializerSettings;
        this.client = client;
    }

    public void SetApiKey()
    {
        this.client.AddDefaultHeader("Authorization", $"ApiKey {this.elasticsearchSettings.ApiKey}");
    }

    public Task<RestResponse<JObject>> GetIndexMappingAsync(string indexName)
    {
        var request = new RestRequest($"/{indexName}/_mapping", Method.Get);
        return this.client.ExecuteAsync<JObject>(request);
    }

    public Task<RestResponse> CreateSnapshotAsync(string indexName, string snapshotName)
    {
        var request = new RestRequest($"/_snapshot/{this.elasticsearchSettings.SnapshotRepositoryName}/{snapshotName}", Method.Put)
            .AddQueryParameter("wait_for_completion", "true")
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

    public Task<RestResponse<RestoreSnapshotResponse>> RestoreSnapshotAsync(string indexName, string snapshotName)
    {
        var request = new RestRequest($"/_snapshot/{this.elasticsearchSettings.SnapshotRepositoryName}/{snapshotName}/_restore", Method.Post)
            .AddQueryParameter("wait_for_completion", "true")
            .AddJsonBody(new RestoreSnapshotRequest()
            {
                Indices = indexName,
            });
        return this.client.ExecuteAsync<RestoreSnapshotResponse>(request);
    }

    public Task<RestResponse> CreateIndexAsync(string indexName, string mapping)
    {
        var request = new RestRequest($"/{indexName}", Method.Put)
            .AddJsonBody(mapping);
        return this.client.ExecuteAsync(request);
    }

    public Task<RestResponse<ReindexResponse>> ReIndexAsync(string sourceIndexName, string destinationIndexName)
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
            .AddJsonBody(reindexRequest);
        return this.client.ExecuteAsync<ReindexResponse>(request);
    }

    public Task<RestResponse<CountResponse>> GetIndexDocumentCountAsync(string indexName)
    {
        var request = new RestRequest($"/{indexName}/_count", Method.Get);
        return this.client.ExecuteAsync<CountResponse>(request);
    }

    public Task<RestResponse> DeleteIndexAsync(string indexName)
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

        Console.WriteLine($"**Request**: \n{JsonConvert.SerializeObject(requestToLog, this.jsonSerializerSettings)}");
    }
}
