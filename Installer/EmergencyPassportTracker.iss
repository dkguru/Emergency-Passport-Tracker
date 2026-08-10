; Emergency Passport Tracker - Inno Setup script
;
; Replaces the old EPT_Installer Visual Studio setup project, which packaged only
; obj\Release\net9.0-windows\apphost.exe (the SDK's blank launcher stub) and none of the
; application's own files.
;
; Build with:  powershell -ExecutionPolicy Bypass -File ..\build-installer.ps1
; or compile this file directly in the Inno Setup IDE after publishing the app.
;
; Requires Inno Setup 6.3 or later (for ArchitecturesAllowed=x64compatible).
; Download: https://jrsoftware.org/isdl.php

#define MyAppName        "Emergency Passport Tracker"
#define MyAppPublisher   "Jesper Angelo"
#define MyAppExeName     "Emergency Passport Tracker.exe"
#define MyAppUrl         ""

; Version is normally passed in by build-installer.ps1 (/DMyAppVersion=1.1.0), which reads it
; from the .csproj. This default is only used when compiling the script by hand.
#ifndef MyAppVersion
  #define MyAppVersion   "1.1.0"
#endif

; Where 'dotnet publish' put the self-contained build.
#ifndef PublishDir
  #define PublishDir     "..\bin\publish\win-x64"
#endif

#if !FileExists(AddBackslash(SourcePath) + PublishDir + "\" + MyAppExeName)
  #error Published application not found. Run build-installer.ps1, or publish with the Installer-win-x64 profile first.
#endif

[Setup]
; Never change AppId once this has been released - it is what lets a new version replace
; the old one instead of installing alongside it.
AppId={{8F3A6C21-5B47-4E8C-9D2F-71A0E4C8B913}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
#if MyAppUrl != ""
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}
#endif

; Per-user install: no administrator rights needed, which matters on a locked-down
; consulate PC. {autopf} resolves to %LOCALAPPDATA%\Programs under 'lowest'.
PrivilegesRequired=lowest
; No install-scope dialog: normal users always get a per-user install. /ALLUSERS on the
; command line is still available if this ever needs to be deployed machine-wide.
PrivilegesRequiredOverridesAllowed=commandline
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

; The published app is win-x64.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

LicenseFile=..\LICENSE.txt
SetupIconFile=..\Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

OutputDir=Output
OutputBaseFilename=EmergencyPassportTracker-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; Offer to shut the app down rather than demanding a reboot if it is running during an upgrade.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; The whole published folder: the app, its dependencies (iText and friends) and the
; bundled .NET runtime. Debug symbols are excluded.
Source: "{#PublishDir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

; No [UninstallDelete] section on purpose. Uninstall removes only what Setup installed.
; Anything the user or the application created under {app} - including an eptdata.enc and
; its Backups folder, if this was ever run as a portable copy - is left alone.

[Messages]
ConfirmUninstall=Are you sure you want to remove %1?%n%nYour passport records are NOT deleted. They stay in your local application data folder, and will still be there if you reinstall.

[Code]

const
  { ProductCode of the old EPT_Installer MSI, so we can spot a leftover install. }
  OldMsiProductCode = '{9CD4B13E-F519-4B89-9CE7-9B9FDA772148}';

function OldMsiInstalled(): Boolean;
var
  Key: String;
begin
  Key := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldMsiProductCode;
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, Key)
         or RegKeyExists(HKEY_CURRENT_USER, Key);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if OldMsiInstalled() then
  begin
    if MsgBox('An older version installed by the previous EPT_Installer package was found.'
      + #13#10#13#10
      + 'It will not be replaced automatically. You should remove it from '
      + 'Settings > Apps after this installation finishes.'
      + #13#10#13#10
      + 'Continue installing?', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\EmergencyPassportTracker');

    if DirExists(DataDir) then
      MsgBox('{#MyAppName} has been removed.'
        + #13#10#13#10
        + 'Your passport records have been left in place:'
        + #13#10 + DataDir
        + #13#10#13#10
        + 'Delete that folder yourself if you want the data gone as well. '
        + 'It is encrypted, and cannot be read without the access code.',
        mbInformation, MB_OK);
  end;
end;
