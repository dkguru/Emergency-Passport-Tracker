# 02-upgrade-tfm: Update target framework to .NET 10

Update the Emergency Passport Tracker.csproj file to target net10.0-windows. The project is already SDK-style, so this is a straightforward TargetFramework property change. The assessment found one deprecated package (itext7 9.5.0) which should be evaluated for alternatives or updated version during this task.

Windows Forms APIs flagged as "binary incompatible" in the assessment are false positives — these APIs are fully supported in .NET 10 with the windows TFM. The real work is:
- Update TargetFramework from net9.0-windows to net10.0-windows
- Restore packages
- Address the deprecated itext7 package (research latest version or alternatives)
- Build and fix any actual compatibility issues

**Done when**: Project file updated to net10.0-windows, solution builds successfully with zero errors and zero warnings, deprecated itext7 package resolved (updated or replaced).

## Research Findings

### Current State
- Project: Emergency Passport Tracker.csproj
- Current TFM: net9.0-windows (line 5)
- Project Type: WinForms (SDK-style)
- Current Package: itext7 9.5.0 (deprecated, line 62)

### Package Update
- itext7 latest compatible version: **9.7.0**
- Action: Update from 9.5.0 to 9.7.0 (resolves deprecation)

### Build Tool Selection
Per building-projects skill:
- WinForms project with .resx files → use **msbuild.exe** (Visual Studio MSBuild)
- Reason: dotnet build cannot process embedded images in .resx files (MSB3086 errors)

### Changes Required
1. Update TargetFramework property: net9.0-windows → net10.0-windows
2. Update itext7 package: 9.5.0 → 9.7.0
3. Build using msbuild.exe
4. Fix any warnings
