# Code Conventions

## Naming
- **LINQ variables**: Use descriptive names (`downloadItem`, `kvp`) — never single letters (`i`, `x`)
- **Private fields**: `_camelCase` (e.g., `_httpClient`, `_viewModel`)
- **Properties**: PascalCase (e.g., `IsMonitoring`, `DownloadItems`)
- **Local variables**: camelCase (e.g., `activeCount`, `nextItem`)
- **Namespaces**: Align with folder structure

## Structure
- **One class per file** (except tightly coupled trivial helpers)
- **Each exception in its own file** under `Exceptions/`
- Models: `TSRDownloader.Core.Models`
- Services: `TSRDownloader.Core.Services` (interfaces) + impl in same namespace
- ViewModels: `TSRDownloader.UI.ViewModels`
- Converters: `TSRDownloader.UI.Converters`

## Patterns
- **MVVM**: ViewModels use CommunityToolkit.Mvvm `[ObservableProperty]` and `[RelayCommand]`
- **DI**: All services registered via `Host.CreateApplicationBuilder().Services`
- **HttpClient**: Created once in `App.xaml.cs` and passed in; never `new HttpClient()` in service logic
- **Cancellation**: All long-running operations accept `CancellationToken`
- **Thread safety**: `lock (_lock)` for shared state, `ConcurrentDictionary` for active tokens

## Logging
- Inject `ILogger<T>` via constructor
- Use structured logging: `_logger.LogInformation("Message {Variable}", value)`
- Never use `System.Diagnostics.Debug` or `Console.WriteLine`

## Configuration
- Defaults in shipped `appsettings.json`; user values in `settings.json` in the data directory
- `AppConfig` section bound via `IConfiguration.GetSection("AppConfig").Get<AppConfig>()`
- Default download directory: `%USERPROFILE%\Downloads\TSR`

For architecture overview see `mem:core`, for build commands see `mem:suggested_commands`.
