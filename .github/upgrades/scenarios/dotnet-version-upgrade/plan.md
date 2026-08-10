# .NET 10 Upgrade Plan

## Overview

**Target**: Emergency Passport Tracker Windows Forms application  
**Scope**: Single project (Emergency Passport Tracker.csproj), currently targeting net9.0-windows, upgrade to net10.0-windows. ~2,400 lines of code.

### Selected Strategy
**All-at-Once** — Single project upgraded in one atomic operation.  
**Rationale**: Single project, already on modern .NET (net9.0), straightforward TFM bump with no complex dependencies.

## Tasks

### 01-prerequisites: Verify upgrade environment

Ensure the development environment is ready for .NET 10 upgrade. Verify that .NET 10 SDK is installed and compatible with the solution's global.json file (if present). This prevents mid upgrade issues related to SDK mismatches.

**Done when**: .NET 10 SDK verified as installed, global.json file (if exists) confirmed compatible with .NET 10 SDK version.

---

### 02-upgrade-tfm: Update target framework to .NET 10

Update the Emergency Passport Tracker.csproj file to target net10.0-windows. The project is already SDK-style, so this is a straightforward TargetFramework property change. The assessment found one deprecated package (itext7 9.5.0) which should be evaluated for alternatives or updated version during this task.

Windows Forms APIs flagged as "binary incompatible" in the assessment are false positives — these APIs are fully supported in .NET 10 with the windows TFM. The real work is:
- Update TargetFramework from net9.0-windows to net10.0-windows
- Restore packages
- Address the deprecated itext7 package (research latest version or alternatives)
- Build and fix any actual compatibility issues

**Done when**: Project file updated to net10.0-windows, solution builds successfully with zero errors and zero warnings, deprecated itext7 package resolved (updated or replaced).

---

### 03-final-validation: Verify upgrade success

Perform full solution validation after the upgrade. Run the complete build, execute all tests, and verify the application functions correctly on .NET 10. Document any warnings or deferred recommendations for future improvements.

**Done when**: Solution builds cleanly (0 errors, 0 warnings), all tests pass, application launches and core functionality verified.