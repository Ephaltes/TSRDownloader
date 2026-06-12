# Task Completion Checklist

Run these before declaring work done:

```powershell
# 1. Clean build from root
dotnet clean TSRDownloader.slnx
dotnet build TSRDownloader.slnx
dotnet test TSRDownloader.slnx

# 2. Check git status (should have clean diff understanding)
git status

# 3. Verify Serena memories are up to date
serena memories check
```

Build must produce **0 errors and 0 warnings** before claiming completion.

If new files were added, verify they are tracked in git (`git status` shows them as `??` if untracked).
