#define AppName    "FeColorizer"
#define AppVersion "1.0"
#define AppExe     "..\bin\Release\net8.0-windows\win-x64\publish\FeColorizer.exe"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppName}
AppPublisherURL=https://github.com/
DefaultDirName={autopf}\{#AppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=FeColorizer-Setup
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\FeColorizer.exe
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: {#AppExe}; DestDir: "{app}"; Flags: ignoreversion

[Registry]
; --- Directory (folder) context menu ---
Root: HKCR; Subkey: "Directory\shell\FeColorizer_Apply";          ValueType: string; ValueName: "";     ValueData: "Colorize subfolders";               Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\shell\FeColorizer_Apply";          ValueType: string; ValueName: "Icon"; ValueData: """{app}\FeColorizer.exe"",0"
Root: HKCR; Subkey: "Directory\shell\FeColorizer_Apply\command";  ValueType: string; ValueName: "";     ValueData: """{app}\FeColorizer.exe"" --colorize ""%1"""

Root: HKCR; Subkey: "Directory\shell\FeColorizer_Revert";         ValueType: string; ValueName: "";     ValueData: "Remove folder colors";              Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\shell\FeColorizer_Revert";         ValueType: string; ValueName: "Icon"; ValueData: """{app}\FeColorizer.exe"",0"
Root: HKCR; Subkey: "Directory\shell\FeColorizer_Revert\command"; ValueType: string; ValueName: "";     ValueData: """{app}\FeColorizer.exe"" --revert ""%1"""

; --- Drive (D:\, etc.) context menu ---
Root: HKCR; Subkey: "Drive\shell\FeColorizer_Apply";              ValueType: string; ValueName: "";     ValueData: "Colorize subfolders";               Flags: uninsdeletekey
Root: HKCR; Subkey: "Drive\shell\FeColorizer_Apply";              ValueType: string; ValueName: "Icon"; ValueData: """{app}\FeColorizer.exe"",0"
Root: HKCR; Subkey: "Drive\shell\FeColorizer_Apply\command";      ValueType: string; ValueName: "";     ValueData: """{app}\FeColorizer.exe"" --colorize ""%1"""

Root: HKCR; Subkey: "Drive\shell\FeColorizer_Revert";             ValueType: string; ValueName: "";     ValueData: "Remove folder colors";              Flags: uninsdeletekey
Root: HKCR; Subkey: "Drive\shell\FeColorizer_Revert";             ValueType: string; ValueName: "Icon"; ValueData: """{app}\FeColorizer.exe"",0"
Root: HKCR; Subkey: "Drive\shell\FeColorizer_Revert\command";     ValueType: string; ValueName: "";     ValueData: """{app}\FeColorizer.exe"" --revert ""%1"""

[Run]
; Pre-generate all 26 color icons into %AppData%\FeColorizer\icons\ after install
Filename: "{app}\FeColorizer.exe"; Parameters: "--generate-icons"; \
    Flags: runhidden waitproguntilterminated; \
    StatusMsg: "Generating folder icons..."

[UninstallDelete]
; Remove the icon cache created in %AppData%\FeColorizer\
Type: filesandordirs; Name: "{userappdata}\FeColorizer"

[Code]
{ Remove leftover registry keys from pre-installer versions of FeColorizer }
procedure CurStepChanged(CurStep: TSetupStep);
var
  Roots: array[0..1] of string;
  OldVerbs: array[0..1] of string;
  I, J: Integer;
begin
  if CurStep <> ssInstall then Exit;

  Roots[0]    := 'Directory\shell';
  Roots[1]    := 'Drive\shell';
  OldVerbs[0] := 'Colorized_Apply';
  OldVerbs[1] := 'Colorized_Revert';

  for I := 0 to 1 do
    for J := 0 to 1 do
      RegDeleteKeyIncludingSubkeys(HKCR, Roots[I] + '\' + OldVerbs[J]);

  { Very first version used a cascading submenu under this key }
  RegDeleteKeyIncludingSubkeys(HKCR, 'Directory\shell\Colorized');

  { Clean up the manual uninstall entry written by the pre-installer exe }
  RegDeleteKeyIncludingSubkeys(
    HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FeColorizer');
end;
