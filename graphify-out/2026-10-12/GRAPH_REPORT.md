# Graph Report - TSRDownloader  (2026-10-12)

## Corpus Check
- 126 files · ~43,466 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: .props 2, .ico 2, .psd 2)

## Summary
- 1167 nodes · 3141 edges · 61 communities (41 shown, 20 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 451 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `96d9f776`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TSRDownloader.Core.Models
- CancellationToken
- ClipboardHookService
- .Combine
- DownloadService
- AppConfig
- Task
- .ExtractItemId
- Window
- .BuildHost
- Window
- .For
- IReadOnlyList
- MainViewModel
- Fact
- TSRDownloader.TestHelper
- Type
- TsrDownloadClient
- TSRDownloader.UI
- .OnStartup
- TSRDownloader.UI.Tests
- TSRDownloader.E2E.Tests
- TSRDownloader.Core
- release job (windows-latest)
- TSRDownloader.Core.Tests
- TSRDownloader.Integration.Tests
- LocExtension
- release.yml (Release workflow)
- UpdateViewModel
- TSR Downloader App Icon (variant A)
- Download Arrow Into Tray
- Application
- TsrDownloadClientTests.cs
- shouldly
- Fixture
- UpdateCoordinator
- LocalizationManager
- SingleInstance
- InlineData
- DownloadItemViewModel
- Program.cs
- CultureInfo
- DownloadQueueAutosave
- WindowLayout
- DownloadStatus
- .Sample
- ListSort
- AppPaths
- ILogger
- TrayIconService
- TSRUrlParserTests
- UpdateMode
- CLAUDE.md
- Dark.xaml
- Light.xaml

## God Nodes (most connected - your core abstractions)
1. `MainViewModel` - 111 edges
2. `DownloadItem` - 79 edges
3. `Window` - 78 edges
4. `TSRDownloader.Core.Models` - 56 edges
5. `AppConfig` - 46 edges
6. `IDownloadService` - 42 edges
7. `TSRDownloader.Core.Services` - 40 edges
8. `DownloadService` - 39 edges
9. `IConfigService` - 36 edges
10. `TSRDownloader.UI.Services` - 34 edges

## Surprising Connections (you probably didn't know these)
- `DownloadService` --implements--> `Parallel resumable downloads`  [INFERRED]
  TSRDownloader.Core/Services/DownloadService.cs → README.md
- `TsrDownloadClient` --implements--> `TSR download wait / ticket`  [INFERRED]
  TSRDownloader.Core/Services/TsrDownloadClient.cs → README.md
- `TSRDownloader.Core` --implements--> `Automatic zip unpacking`  [EXTRACTED]
  TSRDownloader.Core/TSRDownloader.Core.csproj → README.md
- `TSRDownloader.Core` --implements--> `Download list persistence (queue.json)`  [EXTRACTED]
  TSRDownloader.Core/TSRDownloader.Core.csproj → README.md
- `Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests)` --references--> `DownloadService`  [EXTRACTED]
  docs/testing.md → TSRDownloader.Core/Services/DownloadService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Release pipeline: version, notes, pack, upload** — _github_workflows_release_compute_version, github_scripts_next_version, _github_workflows_release_write_release_notes, _github_workflows_release_vpk_pack, nuget_velopack [EXTRACTED 1.00]
- **Test projects (unit, integration, E2E) with shared TestHelper** — tsrdownloader_testhelper_tsrdownloader_testhelper, tsrdownloader_core_tests_tsrdownloader_core_tests, tsrdownloader_ui_tests_tsrdownloader_ui_tests, tsrdownloader_integration_tests_tsrdownloader_integration_tests, tsrdownloader_e2e_tests_tsrdownloader_e2e_tests [EXTRACTED 1.00]

## Communities (61 total, 20 thin omitted)

### Community 0 - "TSRDownloader.Core.Models"
Cohesion: 0.10
Nodes (6): TSRDownloader.UI.ViewModels, TSRDownloader.UI.Views, TSRDownloader.UI.Localization, TSRDownloader.UI.Services, TSRDownloader.Core.Services, TSRDownloader.Core.Models

### Community 1 - "CancellationToken"
Cohesion: 0.30
Nodes (3): AvailableUpdate, VelopackInfo, UpdateCoordinatorTests

### Community 2 - "ClipboardHookService"
Cohesion: 0.05
Nodes (18): DownloadItemsView, TSRDownloader.UI.Behaviors, FileConflictAction, Overwrite, Skip, GridViewColumnSizing, DownloadList, MainWindow (+10 more)

### Community 3 - ".Combine"
Cohesion: 0.06
Nodes (22): ArchiveExtractionService, ConfigService, Config, IArchiveExtractor, IFileConflictResolver, ZipArchiveExtractor, ArchiveExtractionServiceTests, ConfigServiceTests (+14 more)

### Community 4 - "DownloadService"
Cohesion: 0.10
Nodes (16): Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests), Parallel resumable downloads, DownloadProgress, Percentage, FileConflictResolution, TsrDownloadInfo, DownloadService, Items (+8 more)

### Community 5 - "AppConfig"
Cohesion: 0.05
Nodes (31): AppConfig, CloseToTray, DefaultDownloadDirectory, DeleteZipAfterExtraction, DownloadDirectory, ExtractZipArchives, Language, MainWindowLayout (+23 more)

### Community 6 - "Task"
Cohesion: 0.17
Nodes (4): TsrDownloadClientTests, StubHttpMessageHandler, Requests, TsrProtocolStub

### Community 8 - "Window"
Cohesion: 0.06
Nodes (31): BrowseDownloadDirectoryCommand, CloseToTray, DeleteZipAfterExtraction, DownloadDirectory, DownloadDirectoryError, ExtractZipArchives, HasDownloadDirectoryError, Languages (+23 more)

### Community 9 - ".BuildHost"
Cohesion: 0.29
Nodes (3): LoggingSetup, FileConflictResolver, LoggingSetupTests

### Community 10 - "Window"
Cohesion: 0.04
Nodes (53): ActiveCount, ClearFinishedCommand, CompletedCount, ColumnWidthToCellWidthConverter, CopyUrlsCommand, CopyUrlToClipboardCommand, DownloadItems.Count, ErrorCount (+45 more)

### Community 11 - ".For"
Cohesion: 0.21
Nodes (3): DownloadStatusPresentation, DownloadStatusPresenter, DownloadStatusPresenterTests

### Community 12 - "IReadOnlyList"
Cohesion: 0.21
Nodes (4): AtomicFile, DownloadQueueStore, QueueFile, Items

### Community 13 - "MainViewModel"
Cohesion: 0.05
Nodes (27): MainViewModel, ClearFinishedCommand, CompletedCount, CopyUrlsCommand, DownloadItems, DownloadItemsView, ErrorCount, ExitCommand (+19 more)

### Community 14 - "Fact"
Cohesion: 0.07
Nodes (25): DownloadItem, AddedAt, CompletedAt, DownloadedBytes, ErrorMessage, FileName, FilePath, ItemId (+17 more)

### Community 15 - "TSRDownloader.TestHelper"
Cohesion: 0.22
Nodes (4): TSRDownloader.Integration.Tests, TSRDownloader.E2E.Tests, TSRDownloader.TestHelper, TSRDownloader.Core.Tests

### Community 16 - "Type"
Cohesion: 0.16
Nodes (5): TSRDownloader.UI.Converters, BoolToVisibilityConverter, InverseBoolToVisibilityConverter, ColumnWidthToCellWidthConverter, ConverterTests

### Community 18 - "TsrDownloadClient"
Cohesion: 0.12
Nodes (4): TSR download wait / ticket, InvalidDownloadTicketException, TsrEndpoints, TsrDownloadClient

### Community 19 - "TSRDownloader.UI"
Cohesion: 0.18
Nodes (11): CommunityToolkit.Mvvm, CommunityToolkit.WinUI.Notifications, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Serilog.Extensions.Hosting, Serilog.Settings.Configuration, Serilog.Sinks.File, Clipboard monitoring (+3 more)

### Community 21 - "TSRDownloader.UI.Tests"
Cohesion: 0.18
Nodes (10): Xunit.StaFact, TSRDownloader.UI.Tests, net10.0-windows10.0.19041.0, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit (+2 more)

### Community 22 - "TSRDownloader.E2E.Tests"
Cohesion: 0.22
Nodes (9): TSRDownloader.E2E.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk, TSRDownloader.TestHelper (+1 more)

### Community 23 - "TSRDownloader.Core"
Cohesion: 0.25
Nodes (8): Microsoft.Extensions.Configuration.Abstractions, Microsoft.Extensions.Configuration.Binder, Microsoft.Extensions.Logging.Abstractions, Automatic zip unpacking, Download list persistence (queue.json), User data in %LocalAppData%\TSRDownloader, TSRDownloader.Core, Microsoft.NET.Sdk

### Community 24 - "release job (windows-latest)"
Cohesion: 0.32
Nodes (6): Compute version step, release job (windows-latest), Write release notes step (git log since previous tag), Directory.Build.props, Test stack (xUnit, NSubstitute, Shouldly, coverlet, StaFact), next-version.sh script

### Community 25 - "TSRDownloader.Core.Tests"
Cohesion: 0.25
Nodes (8): TSRDownloader.Core.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 26 - "TSRDownloader.Integration.Tests"
Cohesion: 0.25
Nodes (8): TSRDownloader.Integration.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 28 - "release.yml (Release workflow)"
Cohesion: 0.20
Nodes (6): release.yml (Release workflow), vpk pack / upload (Velopack packaging), Directory.Packages.props (central package management), Manual update/release end-to-end check, Velopack, Automatic updates (Install automatically / Notify only / Off)

### Community 29 - "UpdateViewModel"
Cohesion: 0.11
Nodes (14): CloseCommand, InverseBoolToVisibility, IsBlocked, Message, UpdateWindowService, UpdateViewModel, CancellationToken, IsBlocked (+6 more)

### Community 30 - "TSR Downloader App Icon (variant A)"
Cohesion: 0.50
Nodes (4): Download Arrow Glyph, Dark Navy and Green Color Scheme, TSR Downloader App Icon (variant A), TSR Wordmark

### Community 31 - "Download Arrow Into Tray"
Cohesion: 1.00
Nodes (3): Download Arrow Into Tray, TSR Downloader App Icon (variant B), TSR Wordmark

### Community 32 - "Application"
Cohesion: 0.11
Nodes (21): IsDropDownOpen, Application, Arrow, Bd, Box, Check, ContentSite, DropDownBorder (+13 more)

### Community 36 - "Fixture"
Cohesion: 0.12
Nodes (13): IUpdateService, CurrentVersion, IsInstalled, VelopackUpdateService, CurrentVersion, IsInstalled, Fixture, Config (+5 more)

### Community 37 - "UpdateCoordinator"
Cohesion: 0.16
Nodes (6): IUiDispatcher, IUpdateWindowService, UpdateCoordinator, Mode, WpfDispatcher, ImmediateDispatcher

### Community 38 - "LocalizationManager"
Cohesion: 0.17
Nodes (5): LocalizationManager, Culture, CurrentLanguage, Instance, LocalizationManagerTests

### Community 39 - "SingleInstance"
Cohesion: 0.18
Nodes (3): SingleInstance, IsFirstInstance, SingleInstanceTests

### Community 42 - "DownloadItemViewModel"
Cohesion: 0.17
Nodes (9): DownloadItemViewModel, FileName, IsTransferring, IsWaitingForTransfer, ItemId, Model, Sequence, StatusSortOrder (+1 more)

### Community 45 - "DownloadQueueAutosave"
Cohesion: 0.23
Nodes (3): DownloadQueueAutosave, IDownloadQueueStore, DownloadQueueAutosaveTests

### Community 46 - "WindowLayout"
Cohesion: 0.18
Nodes (7): WindowLayout, ColumnWidths, Height, IsMaximized, Left, Top, Width

### Community 47 - "DownloadStatus"
Cohesion: 0.20
Nodes (9): DownloadStatus, Completed, Downloading, Extracting, ExtractionCompleted, ExtractionFailed, Failed, Queued (+1 more)

### Community 48 - ".Sample"
Cohesion: 0.29
Nodes (3): TransferRateMeter, BytesPerSecond, TransferRateMeterTests

### Community 51 - "ListSort"
Cohesion: 0.29
Nodes (6): ListSort, Name, NewestFirst, OldestFirst, Status, SortOption

### Community 52 - "AppPaths"
Cohesion: 0.25
Nodes (5): AppPaths, DataDirectory, LogDirectory, QueueFile, SettingsFile

### Community 57 - "UpdateMode"
Cohesion: 0.40
Nodes (4): UpdateMode, Automatic, Disabled, NotifyOnly

## Knowledge Gaps
- **250 isolated node(s):** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly` (+245 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 334 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `MainViewModel` to `TSRDownloader.Core.Models`, `ClipboardHookService`, `DownloadService`, `UpdateCoordinator`, `LocalizationManager`, `Fixture`, `.Get`, `.BuildHost`, `Window`, `DownloadItemViewModel`, `IReadOnlyList`, `Fact`, `List`, `RelayCommand`, `ListSort`, `.OnStartup`, `.Open`, `UpdateViewModel`?**
  _High betweenness centrality (0.194) - this node is a cross-community bridge._
- **Are the 32 inferred relationships involving `MainViewModel` (e.g. with `Window` and `ActiveCount`) actually correct?**
  _`MainViewModel` has 32 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` to the rest of the system?**
  _250 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TSRDownloader.Core.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.1048780487804878 - nodes in this community are weakly interconnected._
- **Why does `DownloadItem` connect `Fact` to `CancellationToken`, `TsrDownloadClientTests.cs`, `.Combine`, `DownloadService`, `Task`, `InlineData`, `DownloadItemViewModel`, `IReadOnlyList`, `DownloadQueueAutosave`, `MainViewModel`, `DownloadStatus`, `RelayCommand`, `List`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `DownloadItem` (e.g. with `.Save_Should_StoreStatusByName_When_Written()` and `.Restore_Should_FinishQueuedDownloads_When_AppRestarts()`) actually correct?**
  _`DownloadItem` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Should `ClipboardHookService` be split into smaller, more focused modules?**
  _Cohesion score 0.052083333333333336 - nodes in this community are weakly interconnected._