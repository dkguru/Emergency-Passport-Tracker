# Review of the .NET 10 upgrade

The Copilot upgrade did the main job correctly: `net9.0-windows` → `net10.0-windows`, no code
changes needed, and the build is clean with 0 errors and 0 warnings. The 1,527 "incompatible
API" findings in its assessment were false positives — WinForms and System.Drawing APIs that are
fully supported under a `-windows` TFM. Its own final-validation note says the same.

Two things it missed, and one it got half right.

---

## 1. The publish profile was left on .NET 9 — this is why publishing failed

`Properties\PublishProfiles\Installer-win-x64.pubxml` still contained:

```xml
<TargetFramework>net9.0-windows</TargetFramework>
```

The upgrade tool only edits `.csproj` files, so the profile was never touched. A `<TargetFramework>`
in a publish profile **overrides** the project's, so `dotnet publish` asked for a framework that no
longer exists in the restored assets — `NETSDK1005: Assets file … doesn't have a target for
'net9.0-windows/win-x64'`.

This is not a guess. `Installer-win-x64.pubxml.user` in your zip records the attempt:

```xml
<History>False|2026-08-10T05:37:41.5206353Z||;</History>
```

`False` = the publish failed.

**Fixed by deleting the line rather than editing it.** The project has a single target framework,
so the profile inherits it automatically. There is now nothing left to drift the next time you
move to .NET 11.

---

## 2. The deprecated iText package was not actually resolved

Copilot bumped `itext7` from 9.5.0 to 9.7.0, reporting "Deprecated Dependencies: Resolved".

It isn't. **Every `itext7` release from 8.0.1 onwards is deprecated**, 9.7.0 included. The package
was renamed: `itext7.*` → `itext.*`. What remains at the old id is a 30 KB shim with no assemblies
of its own that simply depends on `itext (>= 9.7.0)`. Bumping the version moves to a newer
*deprecated shim*; the id itself has to change.

**Fixed:**

```xml
<PackageReference Include="itext" Version="9.7.0" />
```

Same assemblies (`itext.kernel.dll`, `itext.layout.dll`, …), same namespaces, same API — the DLL
names never carried the "7". No code changes, and the deprecation warning goes away for good.

---

## 3. Everything downstream still said .NET 9

The upgrade tool has no idea these files exist, so they were left behind:

| File | Was | Now |
|---|---|---|
| `build-installer.ps1` | "Install the .NET 9 SDK" | .NET 10 |
| `build-installer.ps1` | "The .NET 9 runtime is included" | .NET 10 |
| `Installer-win-x64.pubxml` | comment referring to the .NET 9 runtime | .NET 10 |
| `INSTALLER.md`, `README.md` | .NET 9 SDK as a prerequisite | .NET 10 SDK |

References to `net9.0-windows` that *describe the old vdproj*, and the `SYSLIB0060` comment in
`SecurityHelper.cs`, are historically accurate and were left alone.

---

## Also worth knowing

* **Version bumped 1.1.0 → 1.2.0**, so this build is distinguishable in Add/Remove Programs and
  the installer filename. Change it in the `.csproj` if you'd rather number it differently.
* **The `<None Include=".github\upgrades\…">` entries** Copilot added to the `.csproj` are
  harmless. They only make its own report files visible in Solution Explorer. They don't collide
  with the SDK's default globs, because those exclude dot-prefixed folders. Delete them if you
  want the project file tidy.
* **Nothing touched the crypto.** `Rfc2898DeriveBytes.Pbkdf2` behaves identically on .NET 10 —
  same iteration count, same SHA-256, same UTF-8 password encoding — so existing `eptdata.enc`
  files still open with the same code.

---

## Before you ship it

I have no .NET SDK or Windows in this environment, so none of this is compiled or run here.
Worth checking once, in order:

1. `dotnet restore` — confirms the `itext` package id resolves.
2. Build — expect 0 warnings again; a `NU1701`/deprecation warning would mean the package
   reference needs another look.
3. `powershell -ExecutionPolicy Bypass -File .\build-installer.ps1` — this is the step that was
   failing. It should now publish and produce
   `Installer\Output\EmergencyPassportTracker-Setup-1.2.0.exe`.
4. Install it, open your existing data file with the real code, and export a PDF — the PDF path
   is the only place iText is touched.
