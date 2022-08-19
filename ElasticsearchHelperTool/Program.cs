using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace ElasticsearchHelperTool
{
    public static class Program
    {
        private const string ServiceName = "ElasticsearchHelperTool";

        public static async Task Main(string[] args)
        {
            Console.Title = ServiceName;

            string env = Environment.GetEnvironmentVariable("ELASTIC_ENVIRONMENT") ?? "Local";

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{env}.json", false)
                .Build();

            var elasticsearchSettings = config.GetSection("Elasticsearch").Get<ElasticsearchSettings>();
            if (elasticsearchSettings == null)
            {
                throw new Exception("Elasticsearch settings not found");
            }

            if (String.IsNullOrEmpty(elasticsearchSettings.Url))
            {
                throw new Exception("ElasticsearchUrl is not set");
            }

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services.Configure<IConfiguration>(config)
                        .AddSingleton<ElasticsearchSettings>(elasticsearchSettings)
                        .AddSingleton<JsonSerializerSettings>(new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                            NullValueHandling = NullValueHandling.Ignore,
                            Formatting = Formatting.Indented,
                        })
                        .AddSingleton<RestClient>(
                            new RestClient(
                                    new RestClientOptions(elasticsearchSettings.Url)
                                    {
                                        ThrowOnAnyError = false, MaxTimeout = 30000,
                                    })
                                .UseNewtonsoftJson())
                        .AddSingleton<ElasticsearchRestClient>()
                        .AddSingleton<GetIndexMappingService>()
                        .AddSingleton<GetIndexDocumentCountService>()
                        .AddSingleton<CreateIndexService>()
                        .AddSingleton<DeleteIndexService>()
                        .AddSingleton<SnapshotService>()
                        .AddSingleton<ReindexService>()
                        .AddSingleton<UpdateIndexMappingService>()
                        .AddSingleton<ElasticsearchHelperToolService>())
                .Build();

            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            ElasticsearchHelperToolService elasticsearchHelperTool = provider.GetRequiredService<ElasticsearchHelperToolService>();
            await elasticsearchHelperTool.RunAsync(args);
        }
    }
}
