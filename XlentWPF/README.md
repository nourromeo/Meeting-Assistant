# XlentWPF

## How to open

- Open `XlentWPF.csproj` in Visual Studio or another IDE that supports .NET 10.
- Or run from the project folder with `dotnet run`.

## How the app starts

- The app starts in `App.xaml.cs` (`OnStartup`). 
- It loads `appsettings.json`, registers services, and shows `MainWindow`.

## Configuration files

- `appsettings.json` is included and copied to the output folder by the project file.
- It contains `ApiSettings:BaseUrl`. The current file value is:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5239/"
  }
}
```

## Konfiguration uppstart WPF app

- Open the project and run the WPF project (F5 in Visual Studio or `dotnet run`).
- Make sure `appsettings.json` has `ApiSettings:BaseUrl`. 
- The app reads this value on startup and uses it to create the HTTP client for `ISummaryApiService`.
- `SummaryApiService` posts to `api/summary` relative to the base URL. 
- No API key or auth headers are set in the project.

