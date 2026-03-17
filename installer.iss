[Setup]
AppName=AIM X
AppVersion=1.0.0.0
DefaultDirName={autopf}\AIM X
DefaultGroupName=AIM X
OutputDir=setup_output
OutputBaseFilename=AIM-X-Setup

PrivilegesRequired=admin
SetupIconFile=app_icon.ico

[Files]
; 🔥 Dynamic EXE (from GitHub Actions)
Source: "{#GetEnv('EXE_PATH')}"; DestDir: "{app}"; Flags: ignoreversion

; 🔥 Include all other files safely
Source: "AIM-X\AIM-X\Aim X\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AIM X"; Filename: "{app}\Aim X.exe"
Name: "{autodesktop}\AIM X"; Filename: "{app}\Aim X.exe"
