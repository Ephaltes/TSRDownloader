# Suggested Commands

## Build & Run (from project root `d:\Github\TSRDownloader`)
```powershell
# Restore packages
dotnet restore TSRDownloader.slnx

# Build
dotnet build TSRDownloader.slnx

# Clean + Build (if obj cache gets stale)
dotnet clean TSRDownloader.slnx; dotnet build TSRDownloader.slnx

# Test (all projects)
dotnet test TSRDownloader.slnx

# Run (builds and launches the WPF app)
dotnet run --project TSRDownloader.UI/TSRDownloader.UI.csproj
```

## Git (Windows PowerShell)
```powershell
git status
git diff
git add -A
git commit -m "message"
git push
```

## Windows-specific
- PowerShell 5.1 (`powershell.exe`) — use `Remove-Item`, `Get-ChildItem` (not `rm`/`ls` from bash)
- `dotnet` CLI works from PowerShell
- Path separator: `\` in PowerShell, `/` works in dotnet CLI

## Serena
- `serena memories check` — validate memory references
