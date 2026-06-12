# TSRDownloader Core Architecture

## Project Structure (slnx-only)
- `TSRDownloader.slnx` — .NET solution file (no .sln)
- `TSRDownloader.Core/` — class library (net10.0): Models, Services, Helpers, Exceptions; no WPF
- `TSRDownloader.UI/` — WPF app (net10.0-windows): Views, ViewModels, Converters, Behaviors, Localization, Services
- Tests: `TSRDownloader.Core.Tests`, `.UI.Tests`, `.Integration.Tests`, `.E2E.Tests`, shared fakes in `TSRDownloader.TestHelper`; see `docs/testing.md`

## Key Design Decisions
- Shared MSBuild properties (TFM, Nullable, ImplicitUsings=false, VersionPrefix) only in `Directory.Build.props`; package versions only in `Directory.Packages.props`
- Explicit `<Using>` groups per csproj; UI omits `System.Windows.Shapes` so `Path` means `System.IO.Path`
- Settings: defaults in shipped `appsettings.json` (`AppConfig` section); user values in `settings.json` in the data dir (`AppPaths`), normalized/clamped by `ConfigService`
- Persisted files written via `Helpers/AtomicFile` (temp file + move)
- Every UI string needs an entry in both `Resources/Strings.resx` and `Strings.de.resx` (a test enforces key parity)
- MVVM via CommunityToolkit.Mvvm source generators; ListView + GridView for the download grid

## Download pipeline
- `TsrDownloadClient` — TSR protocol (ticket → wait → resolve URL → file), owns the `HttpClient`; resumes `.part` files only on HTTP 206
- `DownloadService` — queue + concurrency; event-driven slot filling (no polling); `Retry`, `Remove`, `Restore`
- `ArchiveExtractionService` — single-worker FIFO extraction via `ZipArchiveExtractor` (zip-slip guarded)
- `DownloadQueueStore` (`queue.json`) + `DownloadQueueAutosave` (debounced save on status changes, flush on dispose); restored at startup by `App.OnStartup` → `MainViewModel.RestoreDownloads`
- `ClipboardService` dedups item IDs (`MarkSeen`/`Forget` keep it in sync with the list)

## Dependency Injection
- `Host.CreateApplicationBuilder()` in `App.xaml.cs`; constructor injection, factories only where a path or delegate is needed
- The download client's `HttpClient` needs cookies enabled (TSR passes the download ticket as a cookie); there is no login/session — all downloads are anonymous
- Background events reach the UI via `IUiDispatcher` (`WpfDispatcher` posts, never blocks the worker)

For full tech stack see `mem:tech_stack`, for conventions see `mem:conventions`.
