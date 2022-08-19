using CommandLine;
using ElasticsearchHelperTool.Extensions;
using ElasticsearchHelperTool.Clients;
using ElasticsearchHelperTool.Config;
using ElasticsearchHelperTool.Enums;

namespace ElasticsearchHelperTool.Services;

public class ElasticsearchHelperToolService
{
    private readonly ElasticsearchSettings elasticsearchSettings;
    private readonly ElasticsearchRestClient elasticsearchRestClient;
    private readonly GetIndexMappingService getIndexMappingService;
    private readonly SnapshotService snapshotService;
    private readonly UpdateIndexMappingService updateIndexMappingService;

    public ElasticsearchHelperToolService(
        ElasticsearchSettings elasticsearchSettings,
        ElasticsearchRestClient elasticsearchRestClient,
        GetIndexMappingService getIndexMappingService,
        SnapshotService snapshotService,
        UpdateIndexMappingService updateIndexMappingService)
    {
        this.elasticsearchSettings = elasticsearchSettings;
        this.elasticsearchRestClient = elasticsearchRestClient;
        this.getIndexMappingService = getIndexMappingService;
        this.snapshotService = snapshotService;
        this.updateIndexMappingService = updateIndexMappingService;
    }

    public async Task RunAsync(string[] args)
    {
        string env = Environment.GetEnvironmentVariable("ELASTIC_ENVIRONMENT") ?? "Local";
        bool isAppRunningLocally = this.elasticsearchSettings.UseLocal;

        Console.WriteLine($"The current environment is {env}");
        Console.WriteLine($"useLocal is: {isAppRunningLocally}");
        Console.WriteLine($"ElasticsearchUrl is: {this.elasticsearchSettings.Url}");
        Console.WriteLine("This app will help you to create and manage Elasticsearch indices and mappings.");

        ActionsToPerform actionToPerform = ActionsToPerform.NotSet;

        // check if app is running in GitHub Actions or AWS CodePipeline
        bool isAppRunningInCi = Environment.GetEnvironmentVariable("CI") == "true" || Environment.GetEnvironmentVariable("CODEBUILD_BUILD_ARN") is not null;

        bool performAction;

        if (!isAppRunningInCi)
        {
            Console.WriteLine("Do you want to continue? (y/n)");
            var userInput = Console.ReadLine()?.Trim().ToLower();
            performAction = userInput == "y";
        }
        else
        {
            // we're running in CI so we don't want to take user input, just automatically run the app
            performAction = true;
        }

        if (performAction)
        {
            // get options from command line
            Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed<CliOptions>(cliOptions =>
                {
                    if (cliOptions.ElasticApiKey is not null)
                    {
                        this.elasticsearchSettings.ApiKey = cliOptions.ElasticApiKey;
                    }

                    actionToPerform = cliOptions.ActionsToPerform;
                });

            if (!isAppRunningInCi)
            {
                // the Api Keys for staging and prod are not stored so they must be entered via cmd line or as user input here
                if ((env is "Staging" or "Production") && String.IsNullOrEmpty(this.elasticsearchSettings.ApiKey))
                {
                    Console.WriteLine($"Please enter the ApiKey for {env}");
                    var apiKey = Console.ReadLine()?.Trim();
                    if (String.IsNullOrEmpty(apiKey) || this.elasticsearchSettings is null)
                    {
                        throw new Exception("ApiKey is required");
                    }

                    this.elasticsearchSettings.ApiKey = apiKey;
                }

                // if we haven't set the action via cmd line then ask the user for input
                if (actionToPerform == ActionsToPerform.NotSet)
                {
                    Console.WriteLine("What would you like to do today?");

                    // print out the list of options
                    foreach (ActionsToPerform actionOption in Enum.GetValues(typeof(ActionsToPerform)))
                    {
                        if (actionOption != ActionsToPerform.NotSet && !String.IsNullOrEmpty(actionOption.GetDescription()))
                        {
                            Console.WriteLine(actionOption.GetDescription());
                        }
                    }

                    var userInput = Console.ReadLine()?.Trim().ToLower();

                    // if user entered a number that exists as an option in ActionsToPerform enum, then set the action
                    if (!String.IsNullOrEmpty(userInput) && Int32.TryParse(userInput, out int userInputAsInt) && Enum.IsDefined(typeof(ActionsToPerform), userInputAsInt) &&
                        Enum.TryParse(userInput, out ActionsToPerform action))
                    {
                        actionToPerform = action;
                    }
                    else
                    {
                        throw new Exception("Invalid input. Please select from the available options");
                    }
                }
            }
            else
            {
                // when running via CI we must set the api key via cmd line for staging or prod
                if ((env is "Staging" or "Production") && String.IsNullOrEmpty(this.elasticsearchSettings.ApiKey))
                {
                    throw new Exception("ApiKey is required for Staging and Production in CI");
                }
                Console.WriteLine("The app is running in CI so actions will be performed automatically");
            }

            // make sure the rest client has the correct settings
            this.elasticsearchRestClient.SetApiKey();

            Console.WriteLine($"Action to perform: {actionToPerform.GetDescription()}");

            switch (actionToPerform)
            {
                case ActionsToPerform.GetIndexMapping:
                    await this.getIndexMappingService.GetIndexV1MappingAsync(true);
                    break;
                case ActionsToPerform.UpdateIndexMapping:
                    await this.updateIndexMappingService.UpdateIndexMappingAsync();
                    break;
                case ActionsToPerform.CreateIndexSnapshot when isAppRunningInCi:
                    throw new Exception("Creating a snapshot is not allowed in CI");
                case ActionsToPerform.CreateIndexSnapshot when isAppRunningLocally:
                    throw new Exception("Creating a snapshot is not allowed when running locally");
                case ActionsToPerform.CreateIndexSnapshot:
                    await this.snapshotService.CreateIndexSnapshotAsync();
                    break;
                case ActionsToPerform.RestoreAndReindexIndexSnapshot when isAppRunningInCi:
                    throw new Exception("Restoring a snapshot is not allowed in CI");
                case ActionsToPerform.RestoreAndReindexIndexSnapshot when isAppRunningLocally:
                    throw new Exception("Restoring a snapshot is not allowed when running locally");
                case ActionsToPerform.RestoreAndReindexIndexSnapshot:
                    await this.RestoreIndexV1SnapshotAndReindexAsync();
                    break;
                case ActionsToPerform.None:
                    Console.WriteLine("No actions have been performed. Exiting...");
                    break;
                case ActionsToPerform.NotSet:
                default:
                    throw new Exception("Invalid action. Nothing to do");
            }
        }
        else
        {
            Console.WriteLine("No actions have been performed. Exiting...");
        }
    }

    private async Task RestoreIndexV1SnapshotAndReindexAsync()
    {
        Console.WriteLine("Please enter the name of the snapshot to restore");

        var snapshotName = Console.ReadLine()?.Trim().ToLower();
        if (String.IsNullOrEmpty(snapshotName))
        {
            throw new Exception("Snapshot name is required");
        }

        await this.snapshotService.RestoreIndexV1SnapshotAndReindexAsync(snapshotName);
    }
}
