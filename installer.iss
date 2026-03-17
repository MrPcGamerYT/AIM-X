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
Source: "AIM-X\AIM-X\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AIM X"; Filename: "{app}\Aim X.exe"
Name: "{autodesktop}\AIM X"; Filename: "{app}\Aim X.exe"
