# Technology Stack

## Core
- **.NET 10.0**
- **C#**: SDK default for .NET 10
- **Target OS**: Windows (net10.0-windows for UI, net10.0 for Core)

## UI Framework
- **WPF** (Windows Presentation Foundation)
- **CommunityToolkit.Mvvm** — MVVM source generators
- **XAML** with custom styles and converters

## Dependency Injection & Hosting
- **Microsoft.Extensions.Hosting** — `Host.CreateApplicationBuilder()`
- **Microsoft.Extensions.DependencyInjection**
- **Microsoft.Extensions.Configuration** — appsettings.json via hosting pipeline
- **Microsoft.Extensions.Configuration.Binder** — `Get<T>()` for config binding

## Logging
- **Serilog** — structured logging
- **Serilog.Extensions.Hosting** — integrates with `ILogger<T>`
- **Serilog.Sinks.File** — `%TSRDOWNLOADER_DATA%/logs/tsr-downloader-.log` (data dir, 7 days)
- **Serilog.Settings.Configuration** — config from appsettings.json
- `System.Diagnostics.Debug` is **never used** — always inject `ILogger<T>`

## Build
- **Central Package Management** via `Directory.Packages.props`
- **No ImplicitUsings** — explicit `<Using>` groups in csproj
- `dotnet build` / `dotnet run` from project root

For commands see `mem:suggested_commands`.

## Other
- **Velopack** — installer and auto-update from GitHub Releases (`Program.cs` runs it before WPF)
- **CommunityToolkit.WinUI.Notifications** — toast notifications
- **Tests**: xUnit, NSubstitute, Shouldly, Xunit.StaFact, coverlet
- Exact versions: `Directory.Packages.props`
