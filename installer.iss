#define MyAppName "ENTcapture2"
#define MyAppExe "ENTcapture2.WinForms.exe"

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "artifacts\publish\win-x64"
#endif

[Setup]
AppId={{A7F93817-8D37-4B03-9433-4BC2BE72C633}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher=Yuki ENT clinic
DefaultDirName={autopf}\YUKI_ENT_CLINIC\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=artifacts\installer
OutputBaseFilename={#MyAppName}_v{#AppVersion}_x64
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppExe}
WizardStyle=modern
PrivilegesRequired=admin

#ifndef SkipSign
  SignedUninstaller=yes
  SignToolRetryCount=3
  SignToolMinimumTimeBetween=1000
  SignTool=MSStore /sha1 89AA6D9BABBAAE6672A34DE9F07E47359389ACF2 /s My /fd SHA256 /tr http://timestamp.digicert.com /td SHA256
#endif

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "ショートカット:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "ENTcapture2を起動する"; Flags: nowait postinstall skipifsilent
