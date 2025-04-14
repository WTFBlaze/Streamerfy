[Setup]
AppId={{4e9b134c-9cec-4e51-8135-916a2adad545}} ; A unique, constant GUID for your app
AppName=Streamerfy
AppVersion=PLACEHOLDER
AllowSameVersion=yes
DefaultDirName={autopf}\Streamerfy
DefaultGroupName=Streamerfy
OutputDir=Output
OutputBaseFilename=StreamerfyInstaller
Compression=lzma
SolidCompression=yes
SetupIconFile=Streamerfy.ico
UpdateUninstallLogAppName=yes

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

function InitializeSetup(): Boolean;
var
  PrevVersion: string;
begin
  // Check if Streamerfy is already installed
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Streamerfy_is1', 'DisplayVersion', PrevVersion) then
  begin
    if CompareStr(PrevVersion, '{#AppVersion}') = 0 then
    begin
      MsgBox('Streamerfy v' + PrevVersion + ' is already installed. You can repair or reinstall it.', mbInformation, MB_OK);
    end
    else
    begin
      MsgBox('An older version of Streamerfy (' + PrevVersion + ') is installed. It will be updated to v{#AppVersion}.', mbInformation, MB_OK);
    end;
  end;
  Result := True;
end;