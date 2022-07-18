using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Enums;
using ElasticsearchHelperTool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElasticsearchHelperTool
{
    public static class Program
    {
        private const string ServiceName = "ElasticHelperTool";

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

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services.AddSingleton<IConfiguration>(config)
                        .AddSingleton<ElasticsearchSettings>(elasticsearchSettings)
                        .AddSingleton<ElasticsearchRestClient>()
                        .AddSingleton<IndexMappingService>())
                        .Build();

            Console.WriteLine($"The current environment is {env}");
            Console.WriteLine($"useLocal is: {elasticsearchSettings?.UseLocal}");
            Console.WriteLine($"ElasticsearchUrl is: {elasticsearchSettings?.Url}");
            Console.WriteLine("This app will help you to create and manage Elasticsearch indices and mappings.");

            bool isAppRunningInCi = Environment.GetEnvironmentVariable("GITHUB_ACTION") == "true";

            bool performAction = false;

            if (!isAppRunningInCi)
            {
                Console.WriteLine("Do you want to continue? (y/n)");
                var userInput = Console.ReadLine()?.Trim().ToLower();
                performAction = userInput == "y";
            }
            else if (isAppRunningInCi)
            {
                // we're running in CI so we don't want to take user input, just automatically run the app
                performAction = true;
            }

            if (performAction)
            {
                var actionToPerform = ActionsToPerform.None;
                if (!isAppRunningInCi)
                {
                    // if we're running the app against Staging or Prod the user must input the APIKey as an extra layer of security
                    if (env is "Staging" or "Production")
                    {
                        Console.WriteLine($"Please enter the ApiKey for {env}");
                        var apiKey = Console.ReadLine()?.Trim();
                        if (String.IsNullOrEmpty(apiKey) || elasticsearchSettings is null)
                        {
                            throw new Exception("ApiKey is required");
                        }

                        elasticsearchSettings.ApiKey = apiKey;
                    }

                    Console.WriteLine("What would you like to do today?");
                    Console.WriteLine("1 - Get the current index mapping - useful for readonly testing");
                    Console.WriteLine("2 - Update the mappings for the index");
                    Console.WriteLine("3 - Restore the latest snapshot");

                    var userInput = Console.ReadLine()?.Trim().ToLower();
                    if (!String.IsNullOrEmpty(userInput) && Enum.TryParse(userInput, out ActionsToPerform action))
                    {
                        actionToPerform = action;
                    }
                    else
                    {
                        throw new Exception("Invalid input. Please select from the available options");
                    }
                }
                else
                {
                    // TODO - get the ApiKey from GitHub secrets
                    // if we're running in CI just assume we want to update the mappings
                    actionToPerform = ActionsToPerform.UpdateIndexMapping;
                }

                switch (actionToPerform)
                {
                    case ActionsToPerform.GetIndexMapping:
                        await GetIndexMapping(host.Services, elasticsearchSettings!.IndexV1Name);
                        break;
                    case ActionsToPerform.UpdateIndexMapping:
                        await UpdateIndexMapping(host.Services);
                        break;
                    case ActionsToPerform.RestoreSnapshot when isAppRunningInCi:
                        throw new Exception("Restore snapshot is not allowed in CI");
                    case ActionsToPerform.RestoreSnapshot:
                        Console.WriteLine("Restoring snapshots has not been implemented yet");
                        break;
                    case ActionsToPerform.None:
                    default:
                        throw new Exception("Invalid action. Nothing to do");
                }
            }
            else
            {
                Console.WriteLine("No actions have been performed. Exiting...");
            }
        }

        private static async Task UpdateIndexMapping(IServiceProvider services)
        {
            using IServiceScope serviceScope = services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            IndexMappingService indexMappingService = provider.GetRequiredService<IndexMappingService>();
            await indexMappingService.UpdateIndexMapping();
        }

        private static async Task GetIndexMapping(IServiceProvider services, string indexName)
        {
            using IServiceScope serviceScope = services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            ElasticsearchRestClient elasticsearchRestClient = provider.GetRequiredService<ElasticsearchRestClient>();
            var mapping = await elasticsearchRestClient.GetIndexMapping(indexName);
            Console.WriteLine($"Response: {mapping.StatusCode} {mapping.Content}");
        }
    }
}
