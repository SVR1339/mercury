; Mercury - GitHub Project Installer for Windows
; Install any GitHub repo in one click

#define MyAppName "Mercury - Universal GitHub & Terminal Installer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Mercury"
#define MyAppURL "https://github.com/svreddy313-dev/mercury"
#define MyAppExeName "UniversalGitHubInstaller.exe"
#define MyCliExeName "mercury.exe"

[Setup]
AppId={{B8E4A2C1-5D3F-4E6A-9B7C-2D1E0F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Mercury
DefaultGroupName=Mercury
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=Mercury-Setup
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
MinVersion=10.0.17763

InfoBeforeFile=docs\INSTALL_README.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to Mercury
WelcomeLabel2=This will install [name/ver] on your computer.%n%nMercury empowers you to:%n%n  • Direct install from any copied PowerShell, CMD, or Terminal command%n  • Clone and run any GitHub repo in one click%n  • Direct access to PowerShell, Command Prompt, and Apple Terminal%n  • Auto-detect project types & install all dependencies automatically%n  • Build, test, and run projects with complete internet and shell access%n%nClick Next to continue.
FinishedHeadingLabel=Installation Complete!
FinishedLabel=Mercury has been installed on your computer.%n%nYou can launch it from your Start Menu or Desktop shortcut.%n%nPaste any GitHub URL or install command to get started!

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop shortcut (recommended)"; GroupDescription: "Shortcuts:"
Name: "addtopath"; Description: "Add Mercury CLI (mercury & ugi) to PATH"; GroupDescription: "Advanced options:"; Flags: unchecked
Name: "contextmenu"; Description: "Add 'Install with Mercury' to right-click menu"; GroupDescription: "Advanced options:"; Flags: unchecked

[Files]
; Main GUI application (self-contained .NET 8 — no runtime needed!)
Source: "publish\gui\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; CLI tools (single-file self-contained)
Source: "publish\cli\mercury.exe"; DestDir: "{app}\cli"; Flags: ignoreversion
Source: "publish\cli\ugi.exe"; DestDir: "{app}\cli"; Flags: ignoreversion
; Icon
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Registry]
; Context menu integration for folders
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Mercury"; ValueType: string; ValueData: "Install with Mercury"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Mercury"; ValueName: "Icon"; ValueType: string; ValueData: """{app}\app.ico"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Mercury\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: contextmenu

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Mercury now"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  Path: string;
  CliDir: string;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('addtopath') then
    begin
      CliDir := ExpandConstant('{app}\cli');
      RegQueryStringValue(HKCU, 'Environment', 'Path', Path);
      if Pos(CliDir, Path) = 0 then
      begin
        if Path <> '' then
          Path := Path + ';';
        Path := Path + CliDir;
        RegWriteStringValue(HKCU, 'Environment', 'Path', Path);
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Path: string;
  CliDir: string;
  P: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    CliDir := ExpandConstant('{app}\cli');
    if RegQueryStringValue(HKCU, 'Environment', 'Path', Path) then
    begin
      P := Pos(';' + CliDir, Path);
      if P > 0 then
      begin
        Delete(Path, P, Length(CliDir) + 1);
        RegWriteStringValue(HKCU, 'Environment', 'Path', Path);
      end
      else
      begin
        P := Pos(CliDir + ';', Path);
        if P > 0 then
        begin
          Delete(Path, P, Length(CliDir) + 1);
          RegWriteStringValue(HKCU, 'Environment', 'Path', Path);
        end
        else
        begin
          P := Pos(CliDir, Path);
          if P > 0 then
          begin
            Delete(Path, P, Length(CliDir));
            RegWriteStringValue(HKCU, 'Environment', 'Path', Path);
          end;
        end;
      end;
    end;
  end;
end;
