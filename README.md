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

Command line options are available

| Option   | Description                                                                                | Example |
|----------|--------------------------------------------------------------------------------------------|---------|
| --apiKey | Valid Elasticsearch Api Key for your environment                                           | string  |
| --action | The action you want to perform. This corresponds to the available actions 0-4 listed below | 2       |

For example to return the index mapping for Staging:

`dotnet run --launch-profile="Staging" --apiKey=YOURKEY --action=1`

To update the index mapping for Development:

`dotnet run --launch-profile="Development" --apiKey=YOURKEY --action=2`


If you haven't supplied the action cmd line option you will be asked to enter an option:

0 - Nothing, just exit

1 - Get the current index mapping - useful for readonly testing

2 - Update the mappings for the index

3 - Create a snapshot for the index

4 - Restore a snapshot for the index and reindex

## 1 - Get the current index mapping
This option will just get the current index mapping and print it to console. This is a readonly action that is useful just to check that you're connected correctly 

## 2 - Update the mappings for the index
This option will perform the same steps listed here - [https://stackoverflow.com/c/songtradr/questions/69](https://stackoverflow.com/c/songtradr/questions/69)

Before the index is updated a snapshot will be taken with the name `$"helper-tool-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}"`

This can be restored if anything goes wrong

If you are manually running the app against Staging or Production you will be asked to paste in the ApiKey as an extra safety check

## 3 - Create a snapshot for the index
A snapshot will be created with the name `$"helper-tool-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}"`

## 4 - Restore a snapshot for the index and reindex
You will be asked to enter the name of the snapshot to restore

If the snapshot exists the following steps will be performed:

- Restore the snapshot as restored-index-v1
- Delete index-v1
- Create index-v1 with mappings from file
- Reindex from restored-index-v1 to index-v1
- Get document count and verify it matches
- Delete restored-index-v1