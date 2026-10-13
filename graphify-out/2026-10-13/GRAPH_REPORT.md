# Graph Report - TSRDownloader  (2026-10-12)

## Corpus Check
- 131 files · ~43,789 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: .props 2, .ico 2, .psd 2)

## Summary
- 1188 nodes · 3186 edges · 59 communities (44 shown, 15 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 447 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f1c38546`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TSRDownloader.UI.Services
- Task
- ClipboardHookService
- .BuildHost
- DownloadItem
- LocalizationManager
- TsrDownloadClient
- AppConfiguration
- Window
- TSRDownloader.Core.Models
- Window
- DownloadStatus
- WindowLayout
- MainViewModel
- Fact
- TSRDownloader.TestHelper
- GridViewColumnSizing
- .CreateLogger
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
- DownloadItemViewModel
- shouldly
- IUpdateService
- UpdateCoordinator
- SettingsViewModel
- .Get
- CultureInfo
- ColumnWidthToCellWidthConverter
- IThemeService
- TsrDownloadClientTests.cs
- Window
- Window
- .Sample
- ConfigurationService
- IStartupService
- LocalizeExtension
- DownloadQueueStoreTests
- ListSort
- ArchiveExtractionService
- UpdateMode
- .ExtractionFolder
- WpfDispatcher
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
10. `TSRDownloader.UI.Services` - 36 edges

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

## Communities (59 total, 15 thin omitted)

### Community 0 - "TSRDownloader.UI.Services"
Cohesion: 0.10
Nodes (5): TSRDownloader.UI.Presentation, TSRDownloader.UI.ViewModels, TSRDownloader.UI.Views, TSRDownloader.UI.Localization, TSRDownloader.UI.Services

### Community 1 - "Task"
Cohesion: 0.06
Nodes (22): DownloadProgress, Percentage, FileConflictResolution, TsrDownloadInfo, IArchiveExtractionService, ITsrDownloadClient, ZipArchiveExtractor, DownloadServiceTests (+14 more)

### Community 2 - "ClipboardHookService"
Cohesion: 0.08
Nodes (6): Program, ClipboardHookService, SingleInstance, IsFirstInstance, TrayIconService, SingleInstanceTests

### Community 3 - ".BuildHost"
Cohesion: 0.15
Nodes (3): App, Services, NotificationService

### Community 4 - "DownloadItem"
Cohesion: 0.05
Nodes (25): Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests), Parallel resumable downloads, AtomicFile, DownloadItem, AddedAt, CompletedAt, DownloadedBytes, ErrorMessage (+17 more)

### Community 5 - "LocalizationManager"
Cohesion: 0.17
Nodes (5): LocalizationManager, Culture, CurrentLanguage, Instance, LocalizationManagerTests

### Community 6 - "TsrDownloadClient"
Cohesion: 0.09
Nodes (8): TSR download wait / ticket, InvalidDownloadTicketException, TsrEndpoints, TsrDownloadClient, TsrDownloadClientTests, StubHttpMessageHandler, Requests, TsrProtocolStub

### Community 7 - "AppConfiguration"
Cohesion: 0.13
Nodes (15): AppConfiguration, CloseToTray, DefaultDownloadDirectory, DeleteZipAfterExtraction, DownloadDirectory, ExtractZipArchives, Language, MainWindowLayout (+7 more)

### Community 8 - "Window"
Cohesion: 0.05
Nodes (33): BrowseDownloadDirectoryCommand, CloseToTray, DeleteZipAfterExtraction, DownloadDirectory, DownloadDirectoryError, ExtractZipArchives, HasDownloadDirectoryError, Languages (+25 more)

### Community 9 - "TSRDownloader.Core.Models"
Cohesion: 0.12
Nodes (3): TSRDownloader.Core.Helpers, TSRDownloader.Core.Services, TSRDownloader.Core.Models

### Community 10 - "Window"
Cohesion: 0.04
Nodes (50): ActiveCount, ClearFinishedCommand, ColumnWidthToCellWidthConverter, CopyUrlsCommand, CopyUrlToClipboardCommand, DownloadItems.Count, ErrorCount, ExitCommand (+42 more)

### Community 11 - "DownloadStatus"
Cohesion: 0.09
Nodes (13): DownloadStatus, Completed, Downloading, Extracting, ExtractionCompleted, ExtractionFailed, Failed, Queued (+5 more)

### Community 12 - "WindowLayout"
Cohesion: 0.14
Nodes (7): WindowLayout, ColumnWidths, Height, IsMaximized, Left, Top, Width

### Community 13 - "MainViewModel"
Cohesion: 0.05
Nodes (30): MainViewModel, ActiveCount, ClearFinishedCommand, CompletedCount, CopyUrlsCommand, DownloadItems, DownloadItemsView, ErrorCount (+22 more)

### Community 14 - "Fact"
Cohesion: 0.08
Nodes (13): ClipboardService, IClipboardService, IConfigurationService, Configuration, IDownloadService, Items, ClipboardServiceTests, DownloadProgressTests (+5 more)

### Community 15 - "TSRDownloader.TestHelper"
Cohesion: 0.26
Nodes (3): TSRDownloader.Integration.Tests, TSRDownloader.E2E.Tests, TSRDownloader.TestHelper

### Community 16 - "GridViewColumnSizing"
Cohesion: 0.08
Nodes (13): TSRDownloader.UI.Behaviors, FileConflictAction, Overwrite, Skip, GridViewColumnSizing, ApplyToAllCheckBox, MessageText, Window (+5 more)

### Community 18 - ".CreateLogger"
Cohesion: 0.14
Nodes (7): LoggingSetup, AppPaths, DataDirectory, LogDirectory, QueueFile, SettingsFile, LoggingSetupTests

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

### Community 27 - "MainWindow"
Cohesion: 0.18
Nodes (4): DownloadItemsView, DownloadList, MainWindow, ListView

### Community 28 - "release.yml (Release workflow)"
Cohesion: 0.20
Nodes (6): release.yml (Release workflow), vpk pack / upload (Velopack packaging), Directory.Packages.props (central package management), Manual update/release end-to-end check, Velopack, Automatic updates (Install automatically / Notify only / Off)

### Community 29 - "UpdateViewModel"
Cohesion: 0.18
Nodes (7): IUpdateWindowService, UpdateWindowService, UpdateViewModel, CancellationToken, IsBlocked, Message, UpdateWindow

### Community 30 - "TSR Downloader App Icon (variant A)"
Cohesion: 0.50
Nodes (4): Download Arrow Glyph, Dark Navy and Green Color Scheme, TSR Downloader App Icon (variant A), TSR Wordmark

### Community 31 - "Download Arrow Into Tray"
Cohesion: 1.00
Nodes (3): Download Arrow Into Tray, TSR Downloader App Icon (variant B), TSR Wordmark

### Community 32 - "Application"
Cohesion: 0.11
Nodes (21): IsDropDownOpen, Application, Arrow, Bd, Box, Check, ContentSite, DropDownBorder (+13 more)

### Community 34 - "DownloadItemViewModel"
Cohesion: 0.16
Nodes (9): DownloadItemViewModel, FileName, IsTransferring, IsWaitingForTransfer, ItemId, Model, Sequence, StatusSortOrder (+1 more)

### Community 36 - "IUpdateService"
Cohesion: 0.18
Nodes (10): IUpdateService, CurrentVersion, IsInstalled, Fixture, Configuration, Downloads, Notifications, ShownWindows (+2 more)

### Community 37 - "UpdateCoordinator"
Cohesion: 0.36
Nodes (3): IUiDispatcher, UpdateCoordinator, Mode

### Community 38 - "SettingsViewModel"
Cohesion: 0.18
Nodes (7): LanguageOption, SettingsViewModel, HasDownloadDirectoryError, Languages, Themes, UpdateModes, VersionText

### Community 41 - "ColumnWidthToCellWidthConverter"
Cohesion: 0.18
Nodes (4): TSRDownloader.UI.Converters, ColumnWidthToCellWidthConverter, InverseBooleanToVisibilityConverter, ConverterTests

### Community 42 - "IThemeService"
Cohesion: 0.22
Nodes (6): AppTheme, Dark, Light, ThemeOption, IThemeService, ThemeService

### Community 44 - "Window"
Cohesion: 0.24
Nodes (7): ProjectPageLink, ReportProblemLink, VersionText, Window, AboutWindow, TextBlock, Hyperlink

### Community 45 - "Window"
Cohesion: 0.22
Nodes (8): CloseCommand, InverseBooleanToVisibility, IsBlocked, Message, Window, CancelCommand, BooleanToVisibility, Progress

### Community 46 - ".Sample"
Cohesion: 0.31
Nodes (3): TransferRateMeter, BytesPerSecond, TransferRateMeterTests

### Community 47 - "ConfigurationService"
Cohesion: 0.32
Nodes (3): ConfigurationService, Configuration, QueuePersistenceTests

### Community 51 - "ListSort"
Cohesion: 0.29
Nodes (6): ListSort, Name, NewestFirst, OldestFirst, Status, SortOption

### Community 52 - "ArchiveExtractionService"
Cohesion: 0.12
Nodes (14): ArchiveExtractionService, IArchiveExtractor, IFileConflictResolver, ArchiveExtractionServiceTests, Harness, Clipboard, Configuration, DownloadDirectory (+6 more)

### Community 53 - "UpdateMode"
Cohesion: 0.33
Nodes (5): UpdateMode, Automatic, Disabled, NotifyOnly, UpdateModeOption

## Knowledge Gaps
- **253 isolated node(s):** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly` (+248 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 340 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **15 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `MainViewModel` to `TSRDownloader.UI.Services`, `Task`, `ClipboardHookService`, `.BuildHost`, `DownloadItem`, `LocalizationManager`, `UpdateCoordinator`, `.Get`, `IUpdateService`, `DownloadItemViewModel`, `Window`, `Fact`, `RelayCommand`, `ListSort`, `.ExtractionFolder`, `MainWindow`, `UpdateViewModel`?**
  _High betweenness centrality (0.199) - this node is a cross-community bridge._
- **Are the 32 inferred relationships involving `MainViewModel` (e.g. with `Window` and `ActiveCount`) actually correct?**
  _`MainViewModel` has 32 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` to the rest of the system?**
  _253 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TSRDownloader.UI.Services` be split into smaller, more focused modules?**
  _Cohesion score 0.10317460317460317 - nodes in this community are weakly interconnected._
- **Why does `DownloadItem` connect `DownloadItem` to `Task`, `DownloadItemViewModel`, `TSRDownloader.Core.Models`, `DownloadStatus`, `MainViewModel`, `Fact`, `RelayCommand`, `DownloadQueueStoreTests`, `ArchiveExtractionService`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `DownloadItem` (e.g. with `.Save_Should_StoreStatusByName_When_Written()` and `.Restore_Should_FinishQueuedDownloads_When_AppRestarts()`) actually correct?**
  _`DownloadItem` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.061915729575938216 - nodes in this community are weakly interconnected._