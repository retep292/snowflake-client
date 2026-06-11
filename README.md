# Snowflake Client

A small unpackaged WinUI 3 desktop client for testing Snowflake connectivity with the `Snowflake.Data` NuGet package.

## Requirements

- Windows
- .NET SDK 10

The app is built as an unpackaged, self-contained x64 desktop app, so it does not require a separately installed Windows App Runtime for normal x64 builds.

## Build

```powershell
dotnet build .\SnowflakeClient.slnx
```

## Run

```powershell
dotnet run --project .\SnowflakeClient\SnowflakeClient.csproj
```

Paste a Snowflake connection string, enter a query such as `select current_version();`, then choose **Run**.
