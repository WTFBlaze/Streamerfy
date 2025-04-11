; -- Streamerfy Inno Setup Script --

[Setup]
AppName=Streamerfy
AppVersion=1.0.0
DefaultDirName={pf}\Streamerfy
DefaultGroupName=Streamerfy
OutputDir=Output
OutputBaseFilename=StreamerfyInstaller
Compression=lzma
SolidCompression=yes
DisableWelcomePage=no
UninstallDisplayIcon={app}\Streamerfy.exe
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\InstallerPayload\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Streamerfy"; Filename: "{app}\Streamerfy.exe"
Name: "{group}\Uninstall Streamerfy"; Filename: "{uninstallexe}"
