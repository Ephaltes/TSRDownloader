# TSRDownloader

A small Windows app that downloads mods from [The Sims Resource](https://www.thesimsresource.com)
for you. Copy a TSR link anywhere — browser, chat, a text file — and TSRDownloader picks it up from
the clipboard, waits out TSR's download timer, downloads the file and unpacks it.

## Features

- **Clipboard monitoring** — copied TSR links are queued automatically. Several links at once
  (one per line, or in a sentence) work too; a link already in the list is not added twice.
  Links can also be pasted into the window with <kbd>Ctrl</kbd>+<kbd>V</kbd> or dragged in.
- **Parallel downloads** — 1 to 10 at a time (default 3); interrupted downloads resume from
  where they stopped. A failed download is retried automatically up to 3 times (after 5, 15 and
  45 seconds). Each download shows its size, speed and time left.
- **Never overwrites** — if a file of the same name already exists, the new one is saved as
  `name (2).zip`, `name (3).zip`, …
- **Automatic unpacking** — `.zip` downloads are extracted into a folder of the same name, one
  archive at a time; the `.zip` can be deleted afterwards. If a file already exists you choose
  *Overwrite* or *Skip* (optionally for the whole archive).
- **Remembers the list** — the download list is saved continuously. Close the app at any time:
  unfinished downloads and extractions continue on the next start, finished ones stay as history
  until you clear them. Search and sort it from the toolbar.
- **Runs in the background** — closing the window keeps the app in the notification area;
  starting it a second time brings the running window back.
- **Notifications** (link found, all downloads finished), **light and dark theme**,
  **start with Windows**, **English and German** UI, and **automatic updates**.

## Installation

Download `TSRDownloader-win-Setup.exe` from the latest
[release](https://github.com/Ephaltes/TSRDownloader/releases/latest) and run it. It installs
to `%LocalAppData%\TSRDownloader`, creates Start menu and desktop shortcuts, and installs the
.NET 10 Desktop Runtime if it is missing. The installer is not code-signed, so Windows
SmartScreen may ask for confirmation on first install.

## Usage

1. Start TSRDownloader. Monitoring is on as soon as the window opens.
2. Copy a link to an item on thesimsresource.com. It appears in the list and downloads.

The two status pills at the top right show what is running; click one to pause or resume it:

| Pill | What it does |
|---|---|
| **Clipboard** | Watches the clipboard for TSR links. While paused, copied links are ignored (pasting or dragging still works). |
| **Downloads** | Runs the downloads. While paused, new links are still added to the list and wait. |

Toolbar:

| Button | What it does |
|---|---|
| **Retry failed** | Downloads every failed item again. |
| **Clear finished** | Removes completed items from the list (the files stay on disk). |
| **Open download folder** | Opens the download folder in Explorer. |
| **Search** / sort | Filters the list by file name, link or id; orders it by date added, status or name. |

Right-click an item (or select it and press <kbd>Del</kbd>). Select several with
<kbd>Ctrl</kbd>/<kbd>Shift</kbd>+click or <kbd>Ctrl</kbd>+<kbd>A</kbd>:

- **Retry** — download the failed items again.
- **Open** — open the extracted folder (or a `.zip` that was not extracted).
- **Show in folder** — select the file, or the extracted folder, in Explorer.
- **Open on TSR** — open the item's page on thesimsresource.com.
- **Copy URL** — also available by double-clicking the URL.
- **Remove** — take the items off the list (cancels them if they are downloading). Copy a link
  again to re-add it. Items waiting for or in extraction can be removed once extraction is done.

Closing the window keeps TSRDownloader running in the notification area (click the icon to open
it again). To quit, use *File → Exit* or *Exit* in the icon's right-click menu.

### Settings

*File → Settings*:

- **General** — language, light or dark theme, notification when a link is found, start with
  Windows, keep running in the notification area when the window is closed.
- **Downloads** — download folder (default `%UserProfile%\Downloads\TSR`), number of parallel
  downloads, the wait before a download starts (TSR requires one; raise it if downloads fail with
  *invalid download ticket*), and whether to extract and then delete `.zip` files.
- **Updates** — see below.

## Updates

On startup the app checks GitHub Releases for a newer version. *Settings → Updates*:

- **Install automatically** (default) — downloads and applies the update, then restarts.
- **Notify only** — shows a notification with an *Install now* button.
- **Off** — never checks.

## Where your data lives

Everything is kept in `%LocalAppData%\TSRDownloader`, outside the program folder, so it survives
updates (and is removed on uninstall):

| File | Contents |
|---|---|
| `settings.json` | Your settings and the main window's size, position and column widths. Defaults ship in `appsettings.json` next to the exe. |
| `queue.json` | The download list, restored on the next start. An unreadable file is renamed to `queue.json.corrupt` instead of being overwritten. |
| `logs\` | Daily log files, kept for 7 days. Check these first when something goes wrong. |

A development build keeps these files next to its binaries instead.

## Building from source

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet build TSRDownloader.slnx
dotnet run --project TSRDownloader.UI/TSRDownloader.UI.csproj
dotnet test TSRDownloader.slnx
```

| Project | Purpose |
|---|---|
| `TSRDownloader.Core` | Platform-independent logic: TSR protocol (`TsrDownloadClient`), download queue (`DownloadService`), extraction, settings and queue persistence. |
| `TSRDownloader.UI` | WPF app (MVVM with CommunityToolkit.Mvvm): windows, view models, clipboard hook, notifications, updates. Entry point is `Program.cs` (Velopack runs first). |
| `*.Tests`, `TSRDownloader.TestHelper` | Unit, integration and end-to-end tests — see [docs/testing.md](docs/testing.md). |

Shared build settings live in `Directory.Build.props`, package versions in
`Directory.Packages.props` (central package management).

## Releases

Every push to `main` (except docs-only changes) runs `.github/workflows/release.yml`, which
tests, builds and publishes release `v<Major.Minor>.<n>`. `Major.Minor` is `VersionPrefix`
in `Directory.Build.props`; `<n>` counts up automatically and restarts at 0 when the prefix
changes.
