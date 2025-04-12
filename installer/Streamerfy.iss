[Setup]
AppName=Streamerfy
AppVersion=PLACEHOLDER
DefaultDirName={autopf}\Streamerfy
DefaultGroupName=Streamerfy
OutputDir=Output
OutputBaseFilename=StreamerfyInstaller
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Main app files
Source: "InstallerPayload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; .NET Runtime installer
Source: "InstallerPayload\windowsdesktop-runtime-6.0.36-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

; VC++ Redistributable installer
Source: "InstallerPayload\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\Streamerfy"; Filename: "{app}\Streamerfy.exe"
Name: "{group}\Uninstall Streamerfy"; Filename: "{uninstallexe}"

[Run]
; Install Visual C++ Redistributable if missing
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Visual C++ Redistributable..."; Check: NeedsVC

; Install .NET Desktop Runtime if missing
Filename: "{tmp}\windowsdesktop-runtime-6.0.36-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 6 Desktop Runtime..."; Check: NeedsDotNet

; Launch app (optional)
Filename: "{app}\Streamerfy.exe"; Description: "{cm:LaunchProgram,Streamerfy}"; Flags: nowait postinstall skipifsilent

[Code]
function NeedsDotNet(): Boolean;
begin
    // Checks for the presence of .NET 6.0.36 Desktop Runtime
    Result := not RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft .NET Runtime - 6.0.36 (x64)');
end;

function NeedsVC(): Boolean;
begin
  // Checks for VC++ Redistributable 2015–2022
  Result := not RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum');
end;