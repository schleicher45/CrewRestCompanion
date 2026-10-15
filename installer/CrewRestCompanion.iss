; -------------------------------------------------------------------
;  Crew Rest Companion - Setup
;  Benoetigt Inno Setup 6 oder neuer: https://jrsoftware.org/isdl.php
;  Vorher einmal ..\build.bat ausfuehren, damit dist\ gefuellt ist.
; -------------------------------------------------------------------

#define AppName        "Crew Rest Companion"
; Version kann beim Aufruf ueberschrieben werden: ISCC.exe /DAppVersion=1.3.0 ...
#ifndef AppVersion
  #define AppVersion   "1.2.0"
#endif
#define AppPublisher   "schleicher45"
#define AppUrl         "https://github.com/schleicher45/CrewRestCompanion"
#define ExeName        "CrewRestConfigurator.exe"

[Setup]
AppId={{B7E4C1A2-6D3F-4E58-9C21-5A7F0D8E3B44}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases

; Installation im Benutzerprofil: keine Administratorrechte noetig.
; Der vPilot-Plugins-Ordner liegt ohnehin im Profil des Benutzers.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\CrewRestCompanion
DisableProgramGroupPage=yes
DisableDirPage=auto
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#AppName}

OutputDir=Output
OutputBaseFilename=CrewRestCompanion-Setup-{#AppVersion}
SetupIconFile=..\configurator\app.ico
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "..\dist\CrewRestConfigurator.exe"; DestDir: "{app}"; Flags: ignoreversion
; Die DLL wird bewusst NICHT direkt nach vPilot kopiert - das macht das
; Konfigurationsprogramm, weil es Sicherung, Aufraeumen und Entsperren beherrscht
; und merkt, wenn vPilot noch laeuft.
Source: "..\dist\CrewRestCompanion.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Verknuepfung auf dem Desktop anlegen"; Flags: unchecked

[Run]
Filename: "{app}\{#ExeName}"; Description: "Crew Rest Companion jetzt einrichten"; \
    Flags: postinstall nowait skipifsilent

[Code]
// Beim Deinstallieren fragen, ob das Plugin und die Einstellungen mitsollen.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  PluginDir: string;
  ConfigDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    PluginDir := ExpandConstant('{localappdata}\vPilot\Plugins');

    if FileExists(PluginDir + '\CrewRestCompanion.dll') then
    begin
      if MsgBox('Soll das Plugin auch aus vPilot entfernt werden?' + #13#10 + #13#10 +
                'Nein = Plugin und Einstellungen bleiben erhalten (empfohlen, wenn du nur ' +
                'aktualisierst).',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        DeleteFile(PluginDir + '\CrewRestCompanion.dll');
        DeleteFile(PluginDir + '\CrewRestCompanion.dll.bak');
        DeleteFile(PluginDir + '\config.txt');
        DeleteFile(PluginDir + '\plugin.log');
        DelTree(PluginDir + '\_alte-versionen', True, True, True);

        ConfigDir := ExpandConstant('{userappdata}\CrewRestCompanion');
        DelTree(ConfigDir, True, True, True);
      end;
    end;
  end;
end;
