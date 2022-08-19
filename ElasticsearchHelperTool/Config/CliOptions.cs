using CommandLine;
using ElasticsearchHelperTool.Enums;

namespace ElasticsearchHelperTool.Config;

public class CliOptions
{
    [Option("apiKey", Required = false, HelpText = "ElasticSearch API Key for your environment")]
    public string? ElasticApiKey { get; set; }

    [Option("action", Required = false, HelpText = "Action to perform on the ElasticSearch cluster. Valid options are: 0-4")]
    public ActionsToPerform ActionsToPerform { get; set; } = ActionsToPerform.NotSet;

}
