# Elasticsearch Helper Tool

A .NET console app to automate processes with Elasticsearch indices

## Setup 
Open the solution

Open Config/ElasticsearchSettings and change the IndexAlias, IndexV1Name, IndexV2Name, and SnapshotRepositoryName

Update the appsettings.{env}.json files with your Elasticsearch Url and ApiKey

Update Mappings/index_mapping.json with your mapping

## Running the app

The app can be ran with default settings (local) with:

`dotnet run`

You can also specify which environment you want to run it on with launch profiles:

`dotnet run --launch-profile "Local"`

`dotnet run --launch-profile "Development"`

`dotnet run --launch-profile "Staging"`

`dotnet run --launch-profile "Production"`

The application will print the environment that you're pointing to and you will be asked if you want to continue y/n

You will then be given options:

1 - Get the current index mapping - useful for readonly testing

2 - Update the mappings for the index

3 - Restore the latest snapshot

### 1 - Get the current index mapping
This option will just get the current index mapping and print it to console. This is a readonly action that is useful just to check that you're connected correctly 

### 2 - Update the mappings for the index
This option will perform the following steps:
- Create a v2 of your index
- Reindex from the current version to v2 (to basically backup all Documents)
- Delete the current index
- Recreate the index with the new mapping
- Reindex from v2 onto the new index
- Delete v2

Before the index is updated a snapshot will be taken with the name `$"helper-tool-{DateTime.Now.ToString("YYYYMMDDHHmmsss")}";`

This can be restored if anything goes wrong

If you are manually running the app against Staging or Production you will be asked to paste in the ApiKey as an extra safety check

### 3 - Restore the latest snapshot
This option has not been implemented yet