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
ShowLanguageDialog=yes

[Languages]
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

; Egendefinert tekst (utenom Inno sine egne innebygde meldinger, som allerede er korrekt
; oversatt av MessagesFile over) MÅ deklareres per språk her - ellers vises den alltid på
; samme språk uansett hva brukeren velger i språkdialogen, siden det bare er ren tekst.
[CustomMessages]
english.DesktopIconDescription=Create a desktop shortcut
english.KeepDataQuestion=Do you also want to delete your career data?%n%1%n%nChoose No to keep it for next time you install.
norwegian.DesktopIconDescription=Opprett snarvei på skrivebordet
norwegian.KeepDataQuestion=Vil du også slette karrieredataene dine?%n%1%n%nVelg Nei for å beholde dem til neste gang du installerer.

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIconDescription}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  // Karrieredata ligger i %LOCALAPPDATA%\LmuCareerTool, utenfor installasjonsmappen -
  // spør før vi eventuelt sletter den, i stedet for å rydde den bort stille ved avinstallering.
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\LmuCareerTool');
    if DirExists(DataDir) then
    begin
      if MsgBox(FmtMessage(CustomMessage('KeepDataQuestion'), [DataDir]),
                mbConfirmation, MB_YESNO) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
