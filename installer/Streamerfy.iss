[Setup]
AppName=Streamerfy
AppVersion=1.0.0
DefaultDirName={autopf}\Streamerfy
DefaultGroupName=Streamerfy
OutputDir=Output
OutputBaseFilename=StreamerfyInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "InstallerPayload\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\Streamerfy"; Filename: "{app}\Streamerfy.exe"
Name: "{group}\Uninstall Streamerfy"; Filename: "{uninstallexe}"