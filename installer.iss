[Setup]
AppName=AIM X
AppVersion=1.0.3
DefaultDirName={autopf}\AIM X
DefaultGroupName=AIM X
OutputDir=setup_output
OutputBaseFilename=AIM-X-Setup

; --- PUBLISHER DETAILS ---
AppPublisher=Mr.Pc Gamer
AppPublisherURL=https://github.com/MrPcGamerYT/Optimizer
VersionInfoCompany=Mr.Pc Gamer
VersionInfoDescription=Optimizer System Setup
VersionInfoVersion=1.0.3
VersionInfoCopyright=Ã‚Â© 2026 Mr.Pc Gamer. All Rights Reserved.

; --- ADMIN RIGHTS ---
PrivilegesRequired=admin

; --- ICON FIX ---
SetupIconFile=Optimizer\app_icon.ico 

[Files]
Source: "{#GetEnv('EXE_PATH')}"; DestDir: "{app}"; Flags: ignoreversion

; include everything from the SAME folder as EXE
Source: "{#GetEnv('EXE_PATH')}\..\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AIM X"; Filename: "{app}\Aim X.exe"
Name: "{autodesktop}\AIM X"; Filename: "{app}\Aim X.exe"

[Registry]
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; \
    ValueType: String; ValueName: "{app}\Aim X.exe"; ValueData: "~ RUNASADMIN"; \
    Flags: uninsdeletevalue

[Run]
Filename: "{app}\Aim X.exe"; Description: "{cm:LaunchProgram,Optimizer}"; Flags: nowait postinstall skipifsilent runascurrentuser

