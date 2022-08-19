using System.ComponentModel;

namespace ElasticsearchHelperTool.Enums;

public enum ActionsToPerform
{
    [Description("-1 - Not set")]
    NotSet = -1,
    [Description("0 - Nothing, just exit")]
    None = 0,
    [Description("1 - Get the current index mapping - useful for readonly testing")]
    GetIndexMapping = 1,
    [Description("2 - Update the mappings for the index")]
    UpdateIndexMapping = 2,
    [Description("3 - Create a snapshot for the index")]
    CreateIndexSnapshot = 3,
    [Description("4 - Restore a snapshot for the index and reindex")]
    RestoreAndReindexIndexSnapshot = 4,

}
