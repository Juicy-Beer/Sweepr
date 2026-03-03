; build the app first:
;   dotnet publish src/Sweepr.App -c Release -r win-x64 --self-contained false -o publish
; then compile this script with inno setup (output goes to ..\dist).

[Setup]
AppName=Sweepr
AppVersion=0.1.0
AppPublisher=Juicy-Beer
SetupIconFile=..\src\Sweepr.App\app.ico
SetupIconFile=..\src\Sweepr.App\app.ico
DefaultDirName={autopf}\Sweepr
DefaultGroupName=Sweepr
OutputDir=..\dist
OutputBaseFilename=Sweepr-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Sweepr.exe

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Sweepr"; Filename: "{app}\Sweepr.exe"
Name: "{autodesktop}\Sweepr"; Filename: "{app}\Sweepr.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Sweepr.exe"; Description: "Launch Sweepr"; Flags: nowait postinstall skipifsilent
