# Graph Report - TSRDownloader  (2026-10-12)

## Corpus Check
- 129 files · ~43,530 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: .props 2, .ico 2, .psd 2)

## Summary
- 1176 nodes · 3169 edges · 47 communities (36 shown, 11 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 447 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b6bd1e9f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TSRDownloader.UI.Services
- CancellationToken
- ClipboardHookService
- TsrDownloadClient
- DownloadItem
- SettingsViewModel
- Task
- AppConfiguration
- Window
- TSRDownloader.Core.Models
- Window
- DownloadItemViewModel
- IReadOnlyList
- MainViewModel
- Fact
- shouldly
- GridViewColumnSizing
- TsrDownloadInfo
- TSRDownloader.UI
- .Combine
- TSRDownloader.UI.Tests
- TSRDownloader.E2E.Tests
- TSRDownloader.Core
- release job (windows-latest)
- TSRDownloader.Core.Tests
- TSRDownloader.Integration.Tests
- MainWindow
- release.yml (Release workflow)
- UpdateViewModel
- TSR Downloader App Icon (variant A)
- Download Arrow Into Tray
- Application
- TSRDownloader.UI.Tests
- AvailableUpdate
- UpdateCoordinator
- SingleInstance
- InlineData
- App.xaml.cs
- ListSort
- .BuildHost
- .ExtractionFolder
- CLAUDE.md
- Dark.xaml
- Light.xaml

## God Nodes (most connected - your core abstractions)
1. `MainViewModel` - 110 edges
2. `DownloadItem` - 78 edges
3. `Window` - 75 edges
4. `TSRDownloader.Core.Models` - 59 edges
5. `AppConfiguration` - 46 edges
6. `IDownloadService` - 42 edges
7. `TSRDownloader.Core.Services` - 40 edges
8. `DownloadService` - 39 edges
9. `IConfigurationService` - 36 edges
10. `TSRDownloader.UI.Services` - 35 edges

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

## Communities (47 total, 11 thin omitted)

### Community 0 - "TSRDownloader.UI.Services"
Cohesion: 0.10
Nodes (5): TSRDownloader.UI.Presentation, TSRDownloader.UI.ViewModels, TSRDownloader.UI.Views, TSRDownloader.UI.Localization, TSRDownloader.UI.Services

### Community 2 - "ClipboardHookService"
Cohesion: 0.10
Nodes (12): FileConflictAction, Overwrite, Skip, ClipboardHookService, ApplyToAllCheckBox, MessageText, Window, FileConflictDialog (+4 more)

### Community 3 - "TsrDownloadClient"
Cohesion: 0.11
Nodes (4): TSR download wait / ticket, InvalidDownloadTicketException, TsrEndpoints, TsrDownloadClient

### Community 4 - "DownloadItem"
Cohesion: 0.07
Nodes (27): Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests), Parallel resumable downloads, DownloadItem, AddedAt, CompletedAt, DownloadedBytes, ErrorMessage, FileName (+19 more)

### Community 5 - "SettingsViewModel"
Cohesion: 0.05
Nodes (16): LanguageOption, LocalizationManager, Culture, CurrentLanguage, Instance, AppVersionText, IStartupService, StartupService (+8 more)

### Community 6 - "Task"
Cohesion: 0.16
Nodes (4): TsrDownloadClientTests, StubHttpMessageHandler, Requests, TsrProtocolStub

### Community 7 - "AppConfiguration"
Cohesion: 0.07
Nodes (26): AppConfiguration, CloseToTray, DefaultDownloadDirectory, DeleteZipAfterExtraction, DownloadDirectory, ExtractZipArchives, Language, MainWindowLayout (+18 more)

### Community 8 - "Window"
Cohesion: 0.06
Nodes (31): BrowseDownloadDirectoryCommand, CloseToTray, DeleteZipAfterExtraction, DownloadDirectory, DownloadDirectoryError, ExtractZipArchives, HasDownloadDirectoryError, Languages (+23 more)

### Community 9 - "TSRDownloader.Core.Models"
Cohesion: 0.14
Nodes (4): TSRDownloader.Core.Helpers, TSRDownloader.Core.Exceptions, TSRDownloader.Core.Services, TSRDownloader.Core.Models

### Community 10 - "Window"
Cohesion: 0.04
Nodes (53): ActiveCount, ClearFinishedCommand, ColumnWidthToCellWidthConverter, CopyUrlsCommand, CopyUrlToClipboardCommand, DownloadItems.Count, ErrorCount, ExitCommand (+45 more)

### Community 11 - "DownloadItemViewModel"
Cohesion: 0.05
Nodes (25): DownloadStatus, Completed, Downloading, Extracting, ExtractionCompleted, ExtractionFailed, Failed, Queued (+17 more)

### Community 12 - "IReadOnlyList"
Cohesion: 0.07
Nodes (14): AtomicFile, WindowLayout, ColumnWidths, Height, IsMaximized, Left, Top, Width (+6 more)

### Community 13 - "MainViewModel"
Cohesion: 0.05
Nodes (27): MainViewModel, ActiveCount, ClearFinishedCommand, CompletedCount, CopyUrlsCommand, DownloadItems, DownloadItemsView, ErrorCount (+19 more)

### Community 14 - "Fact"
Cohesion: 0.09
Nodes (12): ClipboardService, IClipboardService, IConfigurationService, Configuration, IDownloadService, Items, ClipboardServiceTests, INotificationService (+4 more)

### Community 15 - "shouldly"
Cohesion: 0.22
Nodes (4): TSRDownloader.Integration.Tests, TSRDownloader.E2E.Tests, TSRDownloader.TestHelper, TSRDownloader.Core.Tests

### Community 18 - "TsrDownloadInfo"
Cohesion: 0.31
Nodes (3): DownloadProgress, Percentage, TsrDownloadInfo

### Community 19 - "TSRDownloader.UI"
Cohesion: 0.18
Nodes (11): CommunityToolkit.Mvvm, CommunityToolkit.WinUI.Notifications, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Serilog.Extensions.Hosting, Serilog.Settings.Configuration, Serilog.Sinks.File, Clipboard monitoring (+3 more)

### Community 20 - ".Combine"
Cohesion: 0.08
Nodes (17): ConfigurationService, Configuration, ZipArchiveExtractor, ConfigurationServiceTests, DownloadQueueStoreTests, ZipArchiveExtractorTests, Harness, Clipboard (+9 more)

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

### Community 27 - "MainWindow"
Cohesion: 0.18
Nodes (4): DownloadItemsView, DownloadList, MainWindow, ListView

### Community 28 - "release.yml (Release workflow)"
Cohesion: 0.20
Nodes (6): release.yml (Release workflow), vpk pack / upload (Velopack packaging), Directory.Packages.props (central package management), Manual update/release end-to-end check, Velopack, Automatic updates (Install automatically / Notify only / Off)

### Community 29 - "UpdateViewModel"
Cohesion: 0.08
Nodes (21): CloseCommand, InverseBooleanToVisibility, IsBlocked, Message, UpdateWindowService, Fixture, Configuration, Downloads (+13 more)

### Community 30 - "TSR Downloader App Icon (variant A)"
Cohesion: 0.50
Nodes (4): Download Arrow Glyph, Dark Navy and Green Color Scheme, TSR Downloader App Icon (variant A), TSR Wordmark

### Community 31 - "Download Arrow Into Tray"
Cohesion: 1.00
Nodes (3): Download Arrow Into Tray, TSR Downloader App Icon (variant B), TSR Wordmark

### Community 32 - "Application"
Cohesion: 0.11
Nodes (21): IsDropDownOpen, Application, Arrow, Bd, Box, Check, ContentSite, DropDownBorder (+13 more)

### Community 36 - "AvailableUpdate"
Cohesion: 0.19
Nodes (8): AvailableUpdate, VelopackInfo, IUpdateService, CurrentVersion, IsInstalled, VelopackUpdateService, CurrentVersion, IsInstalled

### Community 37 - "UpdateCoordinator"
Cohesion: 0.16
Nodes (6): IUiDispatcher, IUpdateWindowService, UpdateCoordinator, Mode, WpfDispatcher, ImmediateDispatcher

### Community 39 - "SingleInstance"
Cohesion: 0.18
Nodes (3): SingleInstance, IsFirstInstance, SingleInstanceTests

### Community 41 - "InlineData"
Cohesion: 0.07
Nodes (9): TSRDownloader.UI.Converters, TSRUrlParser, DownloadProgressTests, TSRUrlParserTests, ColumnWidthToCellWidthConverter, InverseBooleanToVisibilityConverter, TransferFormat, ConverterTests (+1 more)

### Community 51 - "ListSort"
Cohesion: 0.29
Nodes (6): ListSort, Name, NewestFirst, OldestFirst, Status, SortOption

### Community 52 - ".BuildHost"
Cohesion: 0.06
Nodes (17): ArchiveExtractionService, IArchiveExtractor, IFileConflictResolver, ArchiveExtractionServiceTests, App, Services, LocalizeExtension, Key (+9 more)

## Knowledge Gaps
- **252 isolated node(s):** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly` (+247 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 337 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `MainViewModel` to `TSRDownloader.UI.Services`, `ClipboardHookService`, `DownloadItem`, `SettingsViewModel`, `UpdateCoordinator`, `AvailableUpdate`, `Window`, `DownloadItemViewModel`, `IReadOnlyList`, `Fact`, `RelayCommand`, `ListSort`, `.BuildHost`, `.ExtractionFolder`, `MainWindow`, `UpdateViewModel`?**
  _High betweenness centrality (0.200) - this node is a cross-community bridge._
- **Are the 32 inferred relationships involving `MainViewModel` (e.g. with `Window` and `ActiveCount`) actually correct?**
  _`MainViewModel` has 32 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` to the rest of the system?**
  _252 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TSRDownloader.UI.Services` be split into smaller, more focused modules?**
  _Cohesion score 0.09841269841269841 - nodes in this community are weakly interconnected._
- **Why does `DownloadItem` connect `DownloadItem` to `CancellationToken`, `Task`, `TSRDownloader.Core.Models`, `InlineData`, `DownloadItemViewModel`, `IReadOnlyList`, `MainViewModel`, `Fact`, `RelayCommand`, `.BuildHost`, `.Combine`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `DownloadItem` (e.g. with `.Save_Should_StoreStatusByName_When_Written()` and `.Restore_Should_FinishQueuedDownloads_When_AppRestarts()`) actually correct?**
  _`DownloadItem` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Should `ClipboardHookService` be split into smaller, more focused modules?**
  _Cohesion score 0.1 - nodes in this community are weakly interconnected._