; Compile with scripts/build-installer.ps1 after publishing the self-contained app.
#ifndef AppVersion
  #error AppVersion is required. Run scripts/build-installer.ps1.
#endif
#ifndef PublishDir
  #error PublishDir is required. Run scripts/build-installer.ps1.
#endif
#define ProjectRoot AddBackslash(SourcePath) + ".."

[Setup]
AppId=JuanTool
AppName=JuanTool
AppVersion={#AppVersion}
AppPublisher=JuanTool
AppPublisherURL=https://github.com/Duohewater/JuanTool
DefaultDirName={localappdata}\Programs\JuanTool
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
AppMutex=Local\JuanTool.Singleton
CloseApplications=no
RestartApplications=no
WizardStyle=modern
SetupIconFile={#ProjectRoot}\src\JuanTool\Assets\juantool.ico
UninstallDisplayIcon={app}\JuanTool.exe
LicenseFile={#ProjectRoot}\LICENSE
OutputDir={#ProjectRoot}\artifacts\Installer
OutputBaseFilename=JuanTool-Setup-{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PublishDir}\JuanTool.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "{#ProjectRoot}\src\JuanTool\Assets\README.md"; DestDir: "{app}"; DestName: "ICON-SOURCES.md"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\JuanTool"; Filename: "{app}\JuanTool.exe"
Name: "{autodesktop}\JuanTool"; Filename: "{app}\JuanTool.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\JuanTool.exe"; Description: "Open JuanTool"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  { Keep user settings, and remove only this installation's login startup entry. }
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'JuanTool', StartupCommand) then
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\JuanTool.exe') +
        '" --background') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'JuanTool');
end;
