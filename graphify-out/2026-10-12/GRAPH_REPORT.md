# Graph Report - TSRDownloader  (2026-10-12)

## Corpus Check
- Corpus is ~36,884 words - fits in a single context window. You may not need a graph.

## Summary
- 965 nodes · 2597 edges · 34 communities (27 shown, 7 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 367 edges (avg confidence: 0.81)
- Token cost: 156,091 input · 0 output

## Community Hubs (Navigation)
- Namespaces & UI Wiring
- Update Coordinator & TSR Client
- Clipboard Hook & Dialogs
- Config & Archive Test Fixtures
- Download Service Engine
- Settings & Localization
- Client & Clipboard Tests
- URL Parsing & AppConfig
- Settings Window XAML
- Archive Extraction & Logging
- Main Window XAML
- Download Status Presentation
- Queue Persistence Store
- Main ViewModel State
- DownloadItem Model
- MainViewModel Tests & Service Interfaces
- Value Converters
- GridView Column Sizing
- UI Packages & Velopack Release
- App Startup Host
- UI Test Project & Resx Parity
- Solution & E2E Test Project
- Core Project & Data Features
- Versioning & Release Steps
- Core Tests Project
- Integration Tests Project
- Localization Markup Extension
- Release Workflow & Docs
- Clipboard Dedup Service
- App Icon Variant A
- App Icon Variant B
- MainWindow Border Template

## God Nodes (most connected - your core abstractions)
1. `DownloadItem` - 65 edges
2. `MainViewModel` - 63 edges
3. `Window` - 55 edges
4. `TSRDownloader.Core.Models` - 50 edges
5. `TSRDownloader.Core.Services` - 38 edges
6. `AppConfig` - 36 edges
7. `DownloadService` - 35 edges
8. `IDownloadService` - 32 edges
9. `TSRDownloader.UI.Services` - 29 edges
10. `Window` - 29 edges

## Surprising Connections (you probably didn't know these)
- `Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests)` --references--> `DownloadService`  [EXTRACTED]
  docs/testing.md → TSRDownloader.Core/Services/DownloadService.cs
- `DownloadService` --implements--> `Parallel resumable downloads`  [INFERRED]
  TSRDownloader.Core/Services/DownloadService.cs → README.md
- `TsrDownloadClient` --implements--> `TSR download wait / ticket`  [INFERRED]
  TSRDownloader.Core/Services/TsrDownloadClient.cs → README.md
- `TSRDownloader.Core` --implements--> `Automatic zip unpacking`  [EXTRACTED]
  TSRDownloader.Core/TSRDownloader.Core.csproj → README.md
- `TSRDownloader.UI.Tests` --implements--> `Strings.resx / Strings.de.resx key parity`  [EXTRACTED]
  TSRDownloader.UI.Tests/TSRDownloader.UI.Tests.csproj → docs/testing.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Release pipeline: version, notes, pack, upload** — _github_workflows_release_compute_version, github_scripts_next_version, _github_workflows_release_write_release_notes, _github_workflows_release_vpk_pack, nuget_velopack [EXTRACTED 1.00]
- **Test projects (unit, integration, E2E) with shared TestHelper** — tsrdownloader_testhelper_tsrdownloader_testhelper, tsrdownloader_core_tests_tsrdownloader_core_tests, tsrdownloader_ui_tests_tsrdownloader_ui_tests, tsrdownloader_integration_tests_tsrdownloader_integration_tests, tsrdownloader_e2e_tests_tsrdownloader_e2e_tests [EXTRACTED 1.00]

## Communities (34 total, 7 thin omitted)

### Community 0 - "Namespaces & UI Wiring"
Cohesion: 0.05
Nodes (15): TSRDownloader.Core.Helpers, TSRDownloader.UI.Presentation, TSRDownloader.Integration.Tests, TSRDownloader.UI.ViewModels, TSRDownloader.UI.Views, TSRDownloader.E2E.Tests, TSRDownloader.UI.Localization, TSRDownloader.TestHelper (+7 more)

### Community 1 - "Update Coordinator & TSR Client"
Cohesion: 0.06
Nodes (28): TSR download wait / ticket, TsrEndpoints, DownloadProgress, Percentage, TsrDownloadInfo, TsrDownloadClient, AvailableUpdate, VelopackInfo (+20 more)

### Community 2 - "Clipboard Hook & Dialogs"
Cohesion: 0.05
Nodes (26): CloseCommand, InverseBoolToVisibility, IsBlocked, Message, FileConflictAction, Overwrite, Skip, ClipboardHookService (+18 more)

### Community 3 - "Config & Archive Test Fixtures"
Cohesion: 0.08
Nodes (18): ConfigService, Config, ZipArchiveExtractor, ConfigServiceTests, DownloadQueueStoreTests, ZipArchiveExtractorTests, Harness, Clipboard (+10 more)

### Community 4 - "Download Service Engine"
Cohesion: 0.12
Nodes (12): Parallel resumable downloads, FileConflictResolution, DownloadService, Items, IArchiveExtractionService, ITsrDownloadClient, DownloadServiceTests, FullPipelineE2ETests (+4 more)

### Community 5 - "Settings & Localization"
Cohesion: 0.05
Nodes (18): UpdateMode, Automatic, Disabled, NotifyOnly, LanguageOption, LocalizationManager, CurrentLanguage, Instance (+10 more)

### Community 6 - "Client & Clipboard Tests"
Cohesion: 0.09
Nodes (9): InvalidDownloadTicketException, ClipboardServiceTests, TsrDownloadClientTests, StubHttpMessageHandler, Requests, TsrProtocolStub, AppPathsTests, DownloadItemViewModelTests (+1 more)

### Community 7 - "URL Parsing & AppConfig"
Cohesion: 0.08
Nodes (15): TSRUrlParser, AppConfig, DefaultDownloadDirectory, DeleteZipAfterExtraction, DownloadDirectory, ExtractZipArchives, Language, MaxActiveDownloads (+7 more)

### Community 8 - "Settings Window XAML"
Cohesion: 0.05
Nodes (39): BrowseDownloadDirectoryCommand, DeleteZipAfterExtraction, DisplayName, DownloadDirectory, DownloadDirectoryError, ExtractZipArchives, HasDownloadDirectoryError, IsDropDownOpen (+31 more)

### Community 9 - "Archive Extraction & Logging"
Cohesion: 0.09
Nodes (14): ArchiveExtractionService, IArchiveExtractor, IFileConflictResolver, ArchiveExtractionServiceTests, LoggingSetup, AppPaths, DataDirectory, LogDirectory (+6 more)

### Community 10 - "Main Window XAML"
Cohesion: 0.06
Nodes (34): ActiveCount, ClearFinishedCommand, CompletedCount, ColumnWidthToCellWidthConverter, CopyUrlToClipboardCommand, DownloadItems, DownloadItems.Count, ExitCommand (+26 more)

### Community 11 - "Download Status Presentation"
Cohesion: 0.08
Nodes (17): DownloadStatus, Completed, Downloading, Extracting, ExtractionCompleted, ExtractionFailed, Failed, Queued (+9 more)

### Community 12 - "Queue Persistence Store"
Cohesion: 0.12
Nodes (7): AtomicFile, DownloadQueueAutosave, DownloadQueueStore, QueueFile, Items, IDownloadQueueStore, DownloadQueueAutosaveTests

### Community 13 - "Main ViewModel State"
Cohesion: 0.08
Nodes (21): MainViewModel, ActiveCount, ClearFinishedCommand, CompletedCount, DownloadItems, ExitCommand, FailedCount, IsMonitoring (+13 more)

### Community 14 - "DownloadItem Model"
Cohesion: 0.13
Nodes (12): DownloadItem, AddedAt, CompletedAt, DownloadedBytes, ErrorMessage, FileName, FilePath, ItemId (+4 more)

### Community 15 - "MainViewModel Tests & Service Interfaces"
Cohesion: 0.29
Nodes (6): IClipboardService, IConfigService, Config, IDownloadService, Items, MainViewModelTests

### Community 16 - "Value Converters"
Cohesion: 0.17
Nodes (5): TSRDownloader.UI.Converters, BoolToVisibilityConverter, InverseBoolToVisibilityConverter, ColumnWidthToCellWidthConverter, ConverterTests

### Community 19 - "UI Packages & Velopack Release"
Cohesion: 0.14
Nodes (15): vpk pack / upload (Velopack packaging), Manual update/release end-to-end check, CommunityToolkit.Mvvm, CommunityToolkit.WinUI.Notifications, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Serilog.Extensions.Hosting, Serilog.Settings.Configuration (+7 more)

### Community 20 - "App Startup Host"
Cohesion: 0.17
Nodes (5): Application, Bd, App, Services, Border

### Community 21 - "UI Test Project & Resx Parity"
Cohesion: 0.18
Nodes (10): Xunit.StaFact, TSRDownloader.UI.Tests, net10.0-windows10.0.19041.0, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit (+2 more)

### Community 22 - "Solution & E2E Test Project"
Cohesion: 0.22
Nodes (9): TSRDownloader.E2E.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk, TSRDownloader.TestHelper (+1 more)

### Community 23 - "Core Project & Data Features"
Cohesion: 0.22
Nodes (9): Queue persistence tests (DownloadQueueStoreTests, DownloadQueueAutosaveTests, Restore_*, QueuePersistenceTests), Microsoft.Extensions.Configuration.Abstractions, Microsoft.Extensions.Configuration.Binder, Microsoft.Extensions.Logging.Abstractions, Automatic zip unpacking, Download list persistence (queue.json), User data in %LocalAppData%\TSRDownloader, TSRDownloader.Core (+1 more)

### Community 24 - "Versioning & Release Steps"
Cohesion: 0.32
Nodes (6): Compute version step, release job (windows-latest), Write release notes step (git log since previous tag), Directory.Build.props, Test stack (xUnit, NSubstitute, Shouldly, coverlet, StaFact), next-version.sh script

### Community 25 - "Core Tests Project"
Cohesion: 0.25
Nodes (8): TSRDownloader.Core.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 26 - "Integration Tests Project"
Cohesion: 0.25
Nodes (8): TSRDownloader.Integration.Tests, coverlet.collector, Microsoft.NET.Test.Sdk, NSubstitute, Shouldly, xunit, xunit.runner.visualstudio, Microsoft.NET.Sdk

### Community 30 - "App Icon Variant A"
Cohesion: 0.50
Nodes (4): Download Arrow Glyph, Dark Navy and Green Color Scheme, TSR Downloader App Icon (variant A), TSR Wordmark

### Community 31 - "App Icon Variant B"
Cohesion: 1.00
Nodes (3): Download Arrow Into Tray, TSR Downloader App Icon (variant B), TSR Wordmark

## Knowledge Gaps
- **205 isolated node(s):** `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly` (+200 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 267 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `Main ViewModel State` to `Namespaces & UI Wiring`, `Update Coordinator & TSR Client`, `Clipboard Hook & Dialogs`, `Download Service Engine`, `Settings & Localization`, `Archive Extraction & Logging`, `Main Window XAML`, `Download Status Presentation`, `DownloadItem Model`, `MainViewModel Tests & Service Interfaces`, `Main ViewModel Commands`, `App Startup Host`?**
  _High betweenness centrality (0.158) - this node is a cross-community bridge._
- **Are the 6 inferred relationships involving `DownloadItem` (e.g. with `.Save_Should_StoreStatusByName_When_Written()` and `.Restore_Should_FinishQueuedDownloads_When_AppRestarts()`) actually correct?**
  _`DownloadItem` has 6 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` to the rest of the system?**
  _205 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Namespaces & UI Wiring` be split into smaller, more focused modules?**
  _Cohesion score 0.05440771349862259 - nodes in this community are weakly interconnected._
- **Why does `SettingsViewModel` connect `Settings & Localization` to `Namespaces & UI Wiring`, `URL Parsing & AppConfig`, `Settings Window XAML`, `Archive Extraction & Logging`, `Queue Persistence Store`, `MainViewModel Tests & Service Interfaces`, `Main ViewModel Commands`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Are the 21 inferred relationships involving `MainViewModel` (e.g. with `Window` and `ActiveCount`) actually correct?**
  _`MainViewModel` has 21 INFERRED edges - model-reasoned connections that need verification._
- **Should `Update Coordinator & TSR Client` be split into smaller, more focused modules?**
  _Cohesion score 0.0563487530934704 - nodes in this community are weakly interconnected._