# Testing

## Projects

- `TSRDownloader.TestHelper` — shared fakes, stubs and builders (not a test project).
- `TSRDownloader.Core.Tests` — unit tests for Core services and helpers (download queue, TSR protocol,
  extraction, settings, queue persistence).
- `TSRDownloader.UI.Tests` — unit tests for view models, presenter, converters, localization (including
  that `Strings.resx` and `Strings.de.resx` contain the same keys).
- `TSRDownloader.Integration.Tests` — real internal components; the `ITsrDownloadClient` boundary is faked.
  Includes a simulated restart: the queue is saved by one service stack and finished by a fresh one.
- `TSRDownloader.E2E.Tests` — real `TsrDownloadClient` over a stubbed `HttpMessageHandler` (full pipeline, no network, no GUI).

## Running

```bash
# All tests
dotnet test TSRDownloader.slnx

# A single project
dotnet test TSRDownloader.Core.Tests/TSRDownloader.Core.Tests.csproj

# By name
dotnet test TSRDownloader.slnx --filter FullyQualifiedName~ZipArchiveExtractor

# With coverage (writes coverage.cobertura.xml under each TestResults folder)
dotnet test TSRDownloader.slnx --collect:"XPlat Code Coverage"
```

To turn the cobertura files into an HTML report, install ReportGenerator
(`dotnet tool install -g dotnet-reportgenerator-globaltool`) and run:

```bash
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
```

## Stack

xUnit + NSubstitute + Shouldly + coverlet. UI tests use `Xunit.StaFact` (`[StaFact]`)
where WPF requires an STA thread. All tests are deterministic and headless.

## Updates and releases (manual)

The release workflow (`.github/workflows/release.yml`) and the real Velopack update can only
be checked end to end against GitHub. The update logic itself is covered by
`UpdateCoordinatorTests` against a substituted `IUpdateService`.

Version calculation can be dry-run locally in any clone:

```bash
bash .github/scripts/next-version.sh 1.1   # prints version=1.1.N and previous_tag=...
```

End-to-end check after a release:

1. Push to `main` and wait for the **Release** workflow; it publishes `v1.1.N`.
2. Install `TSRDownloader-win-Setup.exe` from that release.
3. Push another change to `main` and wait for `v1.1.N+1`.
4. **Install automatically** (default): start the app — the update window appears, then the
   app restarts on the new version (Settings shows "Version 1.1.N+1").
5. **Notify only**: repeat 3, start the app — a toast "Update available" appears; *Install now*
   updates and restarts. With a download running, *Install now* shows the "downloads in
   progress" message instead.
6. **Off**: repeat 3, start the app — no check, no toast (see the log).

User data lives in `%LocalAppData%\TSRDownloader` (`settings.json`, `queue.json`, `logs\`) and
survives updates; a development build keeps it next to the binaries.

## Download queue persistence (manual)

Covered automatically by `DownloadQueueStoreTests`, `DownloadQueueAutosaveTests`, the
`Restore_*` tests in `DownloadServiceTests` and `QueuePersistenceTests`. To check it by hand:

1. Copy a few TSR links, close the app while one is still downloading.
2. Start the app again: the list is back, the interrupted item shows *Queued* and continues
   (resuming its `.part` file); finished items keep their status.
3. Replace `queue.json` with invalid text and start the app: the list is empty, the log
   contains a warning, and the old content is preserved as `queue.json.corrupt`.
