#define AppName "Steam Toolkit MRM"
#define AppVersion "1.0.0"
#define AppPublisher "TheKillerMRM"
#define AppExeName "SteamToolkitMRM.exe"

[Setup]
AppId={{1F678485-CFE7-4987-85C7-30EB1FB886AB}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\SteamToolkitMRM
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\WindowsApp\Instalador
OutputBaseFilename=SteamToolkitMRM-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes
SetupIconFile=..\native\Assets\SteamToolkitMRM.ico

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho no ambiente de trabalho"; GroupDescription: "Atalhos:"

[Files]
Source: "..\native\obj\InstallerPayload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\native\obj\InstallerPayload\steamtoolkit-installed.flag"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Abrir {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: string;
  InstalledExecutable: string;
begin
  if CurUninstallStep = usUninstall then
  begin
    if RegQueryStringValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Run', 'SteamToolkitMRM', StartupCommand) then
    begin
      InstalledExecutable := Lowercase(ExpandConstant('{app}\SteamToolkitMRM.exe'));
      if Pos(InstalledExecutable, Lowercase(StartupCommand)) > 0 then
        RegDeleteValue(HKEY_CURRENT_USER,
          'Software\Microsoft\Windows\CurrentVersion\Run', 'SteamToolkitMRM');
    end;
  end;
end;
