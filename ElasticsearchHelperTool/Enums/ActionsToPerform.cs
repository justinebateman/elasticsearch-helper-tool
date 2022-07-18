namespace ElasticsearchHelperTool.Enums;

public enum ActionsToPerform
{
    None = 0,
    GetIndexMapping = 1, // this is useful just to check you're connected
    UpdateIndexMapping = 2,
    RestoreSnapshot = 3,

}
