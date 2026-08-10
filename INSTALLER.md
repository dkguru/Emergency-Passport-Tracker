# Building the installer

## Why the old one stopped working

`EPT_Installer.vdproj` packaged exactly one file:

```
..\obj\Release\net9.0-windows\apphost.exe
```

That file is not the application. `apphost.exe` under `obj\` is the blank native launcher stub
that the .NET SDK copies and patches into `bin\Emergency Passport Tracker.exe` during a build.
On its own it contains no application code and no reference to the managed DLL.

Nothing else was packaged at all — no `Emergency Passport Tracker.dll`, no
`.runtimeconfig.json`, no `.deps.json`, and none of the iText assemblies. Even if the MSI had
built cleanly, the installed program could not have started.

On top of that:

* It used the **`PublishItems` output group**, which the Visual Studio Installer Projects
  extension cannot resolve for SDK-style projects. This is the most likely cause of the build
  failure itself.
* **No .NET prerequisite.** `LaunchCondition` was empty and the bootstrapper listed nothing, so
  a machine without the .NET 9 Desktop Runtime had no way to get it.
* **Product metadata was never filled in** — Manufacturer `Default Company Name`, ProductName
  `EPT_Installer`. That is what would have appeared in Add/Remove Programs, and it produced the
  install path `C:\Program Files\Default Company Name\EPT_Installer`.
* **`RemovePreviousVersions` was FALSE** with a fixed ProductCode, so version upgrades would
  have failed or installed twice.
* **`InstallAllUsers` was FALSE but the target was `[ProgramFilesFolder]`**, with
  `RequiresElevation` FALSE — a per-user install pointed at a machine-wide location, which
  fails outright on a standard user account.
* A Start Menu folder was declared but no shortcut was ever placed in it.

The project type is also deprecated by Microsoft for modern .NET. It has been removed and
replaced with an Inno Setup script.

---

## What you need, once

1. **.NET 9 SDK** — you already have this, since the project builds.
2. **Inno Setup 6.3 or later** — <https://jrsoftware.org/isdl.php>. Free, and the plain
   installer is fine; no extra components are needed.

You do **not** need the Visual Studio Installer Projects extension any more.

---

## Building it

From a PowerShell prompt in the project folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

That does two things:

1. `dotnet publish` with the `Installer-win-x64` profile → `bin\publish\win-x64\`
2. Inno Setup compiles `Installer\EmergencyPassportTracker.iss` → `Installer\Output\`

The result is:

```
Installer\Output\EmergencyPassportTracker-Setup-1.1.0.exe
```

Roughly 60–70 MB, because the .NET runtime is bundled.

### Options

| Command | Effect |
|---|---|
| `.\build-installer.ps1` | Full build |
| `.\build-installer.ps1 -SkipPublish` | Rebuild only the installer, reusing the last publish |
| `.\build-installer.ps1 -Version 1.2.0` | Override the version for this build |

Before the installer is compiled the script checks that the publish output actually contains
the managed DLL, the `runtimeconfig.json`, `hostfxr.dll`, WinForms and iText. That check exists
specifically so the old failure — shipping a package that cannot start — cannot recur silently.

### Releasing a new version

Bump `<Version>` in `Emergency Passport Tracker.csproj` and rebuild. Everything else — the
installer filename, the Add/Remove Programs entry, the exe's file properties — follows from it.

Do **not** change `AppId` in the `.iss`. It is what lets a new installer replace the previous
version instead of installing beside it.

---

## What the installer does

* Installs **per user**, into `%LOCALAPPDATA%\Programs\Emergency Passport Tracker`.
  No administrator rights are needed.
* Bundles the **.NET 9 runtime**, so the target PC needs nothing installed first, and the app
  keeps working if Microsoft's runtime is later updated or removed.
* Creates a Start Menu entry, and a desktop shortcut if the box is ticked.
* Offers to close the app automatically when installing over a running copy.
* Warns if it finds a leftover install from the old MSI, which it cannot remove for you.

### Your data is never touched

Passport records live in:

```
%LOCALAPPDATA%\EmergencyPassportTracker\eptdata.enc
```

That is a **different folder** from the program itself. Installing, upgrading and uninstalling
all leave it completely alone. The uninstaller says so explicitly and tells you where the data
is, in case you want to remove it by hand.

Take a CSV backup before any upgrade anyway.

### Machine-wide install

If this ever needs to go on a shared PC for all users:

```
EmergencyPassportTracker-Setup-1.1.0.exe /ALLUSERS
```

That requires administrator rights. Note that each Windows user still gets their own data file,
because the data lives in each user's local application data.

---

## Notes

* **Windows SmartScreen** will warn on first run, because the installer is not code-signed.
  "More info" → "Run anyway". If the consulate has a code-signing certificate, add
  `SignTool` directives to the `[Setup]` section and the warning goes away.
* The installer is **x64 only**, matching the `win-x64` publish. For an ARM64 Windows machine,
  publish with `-r win-arm64` and change `ArchitecturesAllowed` accordingly.
* `Installer\Output\` and `bin\publish\` are excluded from git via `.gitignore`.
* The application icon is `Resources\app.ico`, generated from `DK-SHIELD_75.jpg`. The source
  image is only 75×75, so the larger icon sizes are upscaled and will look soft. If you have the
  shield artwork at a higher resolution, regenerate the `.ico` from that.
