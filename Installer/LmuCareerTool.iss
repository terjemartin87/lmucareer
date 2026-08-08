; LMU Karriere - Inno Setup-skript.
; Kompilerer forutsetter at `dotnet publish` allerede har lagt en self-contained
; single-file build i ..\publish\ (kjør build-installer.ps1, som gjør begge stegene).
;
; Installasjon er per bruker (PrivilegesRequired=lowest) - ingen admin-rettigheter,
; ingen UAC-prompt, slik at en kompis bare kan laste ned og kjøre installeren.

#define MyAppName "LMU Karriere"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Terje Hognestad"
#define MyAppExeName "LmuCareerTool.App.exe"

[Setup]
AppId={{4C4D5543-4152-4545-522D-544F4F4C3031}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\LmuCareerTool
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=LmuCareerTool-Setup-{#MyAppVersion}
SetupIconFile=..\LmuCareerTool.App\Assets\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Opprett snarvei på skrivebordet"; GroupDescription: "Snarveier:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Avinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Start {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  { Karrieredata ligger i %LOCALAPPDATA%\LmuCareerTool, utenfor {app} - spør før vi
    eventuelt sletter den, i stedet for å rydde den bort stille ved avinstallering. }
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\LmuCareerTool');
    if DirExists(DataDir) then
    begin
      if MsgBox('Vil du også slette karrieredataene dine?' + #13#10 + DataDir + #13#10#13#10 +
                'Velg Nei for å beholde dem til neste gang du installerer.',
                mbConfirmation, MB_YESNO) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
