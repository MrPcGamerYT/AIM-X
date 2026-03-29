#define MyAppName "AIM X"
#define MyAppVersion "1.2.9"
#define MyAppExe "Aim X.exe"
#define MyBuildDir GetEnv("BUILD_OUTPUT")

[Setup]
AppName={#MyAppName}
AppVersion=1.3.1
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=setup_output
OutputBaseFilename=AIM-X-Setup

; Publisher
AppPublisher=Mr.Pc Gamer
AppPublisherURL=https://github.com/MrPcGamerYT/AIM-X
VersionInfoCompany=Mr.Pc Gamer
VersionInfoDescription=Optimizer System Setup
VersionInfoVersion=1.3.1
VersionInfoCopyright=Â© 2026 Mr.Pc Gamer

PrivilegesRequired=admin
SetupIconFile=app_icon.ico

[Files]
; ðŸ”¥ COPY EVERYTHING FROM BUILD OUTPUT
Source: "{#MyBuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; \
ValueType: String; ValueName: "{app}\{#MyAppExe}"; ValueData: "~ RUNASADMIN"; \
Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; \
Flags: nowait postinstall skipifsilent runascurrentuser
