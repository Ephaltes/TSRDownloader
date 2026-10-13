# Graph Report - TSRDownloader  (2026-10-13)

## Corpus Check
- 131 files · ~43,939 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 10 file(s) not represented in the graph (top: .props 2, .ico 2, .psd 2)

## Summary
- 1191 nodes · 3195 edges · 60 communities (40 shown, 20 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 447 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f0808d65`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TSRDownloader.UI.Services
- DownloadService
- ClipboardHookService
- .BuildHost
- IReadOnlyList
- LocalizationManagerTests
- StubHttpMessageHandler
- AppConfiguration
- Window
- TSRDownloader.Core.Models
- Window
- DownloadStatus
- MainWindow
- MainViewModel
- Fact
- TSRDownloader.TestHelper
- GridViewColumnSizing
- AppPaths
- TSRDownloader.UI
- Task
- TSRDownloader.UI.Tests
- TSRDownloader.E2E.Tests
- TSRDownloader.Core
- release job (windows-latest)
- TSRDownloader.Core.Tests
- TSRDownloader.Integration.Tests
- .OnSelectionChanged
- release.yml (Release workflow)
- UpdateViewModel
- TSR Downloader App Icon (variant A)
- Download Arrow Into Tray
- Application
- DownloadItemViewModel
- shouldly
- AvailableUpdate
- UpdateCoordinator
- SettingsViewModel
- .Get
- CancellationToken
- InlineData
- SingleInstance
- App.xaml.cs
- Window
- FileConflictDialog
- .Sample
- DownloadQueueAutosave
- IStartupService
- LocalizeExtension
- TrayIconService
- ListSort
- .Combine
- .CreateLogger
- .ExtractionFolder
- LocalizationManager
- CLAUDE.md
- Dark.xaml
- Light.xaml

## God Nodes (most connected - your core abstractions)
1. `MainViewModel` - 110 edges
2. `DownloadItem` - 78 edges
3. `Window` - 76 edges
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

## Communities (60 total, 20 thin omitted)

### Community 0 - "TSRDownloader.UI.Services"
Cohesion: 0.08
Nodes (5): TSRDownloader.UI.Presentation, TSRDownloader.UI.ViewModels, TSRDownloader.UI.Views, TSRDownloader.UI.Localization, TSRDownloader.UI.Services

### Community 1 - "DownloadService"
Cohesion: 0.09
Nodes (17): Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests), Parallel resumable downloads, DownloadProgress, Percentage, FileConflictResolution, TsrDownloadInfo, DownloadService, Items (+9 more)

### Community 3 - ".BuildHost"
Cohesion: 0.16
Nodes (4): App, Services, FileConflictResolver, NotificationService

### Community 4 - "IReadOnlyList"
Cohesion: 0.20
Nodes (5): AtomicFile, DownloadQueueStore, QueueFile, Items, IDownloadQueueStore

### Community 6 - "StubHttpMessageHandler"
Cohesion: 0.14
Nodes (6): TSRDownloader.Core.Exceptions, InvalidDownloadTicketException, TsrDownloadClientTests, StubHttpMessageHandler, Requests, TsrProtocolStub

### Community 7 - "AppConfiguration"
Cohesion: 0.09
Nodes (21): AppConfiguration, CloseToTray, DefaultDownloadDirectory, DeleteZipAfterExtraction, DownloadDirectory, ExtractZipArchives, Language, MainWindowLayout (+13 more)

### Community 8 - "Window"
Cohesion: 0.06
Nodes (31): BrowseDownloadDirectoryCommand, CloseToTray, DeleteZipAfterExtraction, DownloadDirectory, DownloadDirectoryError, ExtractZipArchives, HasDownloadDirectoryError, Languages (+23 more)

### Community 9 - "TSRDownloader.Core.Models"
Cohesion: 0.13
Nodes (3): TSRDownloader.Core.Helpers, TSRDownloader.Core.Services, TSRDownloader.Core.Models

### Community 10 - "Window"
Cohesion: 0.04
Nodes (53): ActiveCount, ClearFinishedCommand, ColumnWidthToCellWidthConverter, CopyUrlsCommand, CopyUrlToClipboardCommand, DownloadItems.Count, ErrorCount, ErrorToolTip (+45 more)

### Community 11 - "DownloadStatus"
Cohesion: 0.09
Nodes (13): DownloadStatus, Completed, Downloading, Extracting, ExtractionCompleted, ExtractionFailed, Failed, Queued (+5 more)

### Community 13 - "MainViewModel"
Cohesion: 0.05
Nodes (28): MainViewModel, ActiveCount, ClearFinishedCommand, CompletedCount, DownloadItems, DownloadItemsView, ErrorCount, ExitCommand (+20 more)

### Community 14 - "Fact"
Cohesion: 0.07
Nodes (25): DownloadItem, AddedAt, CompletedAt, DownloadedBytes, ErrorMessage, FileName, FilePath, ItemId (+17 more)

### Community 15 - "TSRDownloader.TestHelper"
Cohesion: 0.20
Nodes (4): TSRDownloader.Integration.Tests, TSRDownloader.E2E.Tests, TSRDownloader.TestHelper, TSRDownloader.Core.Tests

### Community 18 - "AppPaths"
Cohesion: 0.25
Nodes (5): AppPaths, DataDirectory, LogDirectory, QueueFile, SettingsFile

### Community 19 - "TSRDownloader.UI"
Cohesion: 0.18
Nodes (11): CommunityToolkit.Mvvm, CommunityToolkit.WinUI.Notifications, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Serilog.Extensions.Hosting, Serilog.Settings.Configuration, Serilog.Sinks.File, Clipboard monitoring (+3 more)

### Community 20 - "Task"
Cohesion: 0.12
Nodes (3): TSR download wait / ticket, TsrEndpoints, TsrDownloadClient

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

### Community 27 - ".OnSelectionChanged"
Cohesion: 0.40
Nodes (3): DownloadItemsView, DownloadList, ListView

### Community 28 - "release.yml (Release workflow)"
Cohesion: 0.20
Nodes (6): release.yml (Release workflow), vpk pack / upload (Velopack packaging), Directory.Packages.props (central package management), Manual update/release end-to-end check, Velopack, Automatic updates (Install automatically / Notify only / Off)

### Community 29 - "UpdateViewModel"
Cohesion: 0.09
Nodes (22): CloseCommand, InverseBooleanToVisibility, IsBlocked, Message, IUpdateWindowService, UpdateWindowService, Fixture, Configuration (+14 more)

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
Cohesion: 0.17
Nodes (9): DownloadItemViewModel, FileName, IsTransferring, IsWaitingForTransfer, ItemId, Model, Sequence, StatusSortOrder (+1 more)

### Community 36 - "AvailableUpdate"
Cohesion: 0.19
Nodes (8): AvailableUpdate, VelopackInfo, IUpdateService, CurrentVersion, IsInstalled, VelopackUpdateService, CurrentVersion, IsInstalled

### Community 37 - "UpdateCoordinator"
Cohesion: 0.11
Nodes (10): UpdateMode, Automatic, Disabled, NotifyOnly, UpdateModeOption, IUiDispatcher, UpdateCoordinator, Mode (+2 more)

### Community 38 - "SettingsViewModel"
Cohesion: 0.16
Nodes (7): LanguageOption, SettingsViewModel, HasDownloadDirectoryError, Languages, Themes, UpdateModes, VersionText

### Community 41 - "InlineData"
Cohesion: 0.05
Nodes (16): TSRDownloader.UI.Converters, TSRUrlParser, WindowLayout, ColumnWidths, Height, IsMaximized, Left, Top (+8 more)

### Community 42 - "SingleInstance"
Cohesion: 0.17
Nodes (4): Program, SingleInstance, IsFirstInstance, SingleInstanceTests

### Community 44 - "Window"
Cohesion: 0.25
Nodes (6): ProjectPageLink, ReportProblemLink, VersionText, Window, TextBlock, Hyperlink

### Community 45 - "FileConflictDialog"
Cohesion: 0.15
Nodes (11): FileConflictAction, Overwrite, Skip, ApplyToAllCheckBox, MessageText, Window, FileConflictDialog, ApplyToAll (+3 more)

### Community 46 - ".Sample"
Cohesion: 0.29
Nodes (3): TransferRateMeter, BytesPerSecond, TransferRateMeterTests

### Community 51 - "ListSort"
Cohesion: 0.22
Nodes (6): ListSort, Name, NewestFirst, OldestFirst, Status, SortOption

### Community 52 - ".Combine"
Cohesion: 0.06
Nodes (21): ArchiveExtractionService, ConfigurationService, Configuration, IArchiveExtractor, IFileConflictResolver, ZipArchiveExtractor, ArchiveExtractionServiceTests, ConfigurationServiceTests (+13 more)

### Community 55 - "LocalizationManager"
Cohesion: 0.40
Nodes (4): LocalizationManager, Culture, CurrentLanguage, Instance

## Knowledge Gaps
- **254 isolated node(s):** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly` (+249 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 341 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `MainViewModel` to `TSRDownloader.UI.Services`, `DownloadService`, `ClipboardHookService`, `.BuildHost`, `IReadOnlyList`, `UpdateCoordinator`, `AvailableUpdate`, `.Get`, `DownloadItemViewModel`, `Window`, `MainWindow`, `Fact`, `RelayCommand`, `ListSort`, `.ExtractionFolder`, `LocalizationManager`, `.OnStartup`, `UpdateViewModel`?**
  _High betweenness centrality (0.201) - this node is a cross-community bridge._
- **Are the 32 inferred relationships involving `MainViewModel` (e.g. with `Window` and `ActiveCount`) actually correct?**
  _`MainViewModel` has 32 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` to the rest of the system?**
  _254 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TSRDownloader.UI.Services` be split into smaller, more focused modules?**
  _Cohesion score 0.08416389811738649 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `.OnSelectionChanged`, `MainWindow`, `MainViewModel`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `DownloadItem` (e.g. with `.Save_Should_StoreStatusByName_When_Written()` and `.Restore_Should_FinishQueuedDownloads_When_AppRestarts()`) actually correct?**
  _`DownloadItem` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Should `DownloadService` be split into smaller, more focused modules?**
  _Cohesion score 0.09045045045045046 - nodes in this community are weakly interconnected._