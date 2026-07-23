; Snap.Hutao.Remastered — Inno Setup Script
; 从 Snap.Hutao.Remastered 仓库根目录执行以下命令:
;
; 1. 构建并发布到 Installer\Publish:
;   dotnet build "src\Snap.Hutao.Remastered\Snap.Hutao.Remastered\Snap.Hutao.Remastered.csproj" -c Release --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true
;   (然后复制 bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\* 到 Installer\Publish\)
;
; 2. 编译安装程序:
;   iscc Installer\installer.iss

#define MyAppName "Snap.Hutao.Remastered"
#define MyAppShortName "Snap.Hutao.Remastered"
#ifndef MyAppVersion
  #define MyAppVersion "1.19.4.0"
#endif
#define MyAppPublisher "SnapHutaoRemasteringProject"
#define MyAppURL "https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered"
#define MyAppExeName "Snap.Hutao.Remastered.exe"
#define MyAppAssocName "Hutao Protocol"
#define MyAppAssocExt ".hutao"
#define MyAppAssocKey "hutao"
#define CodeSigningCertificateFileName "SnapHutaoRemasteringProjectCodeSigning.cer"

#define PublishDir "Publish"
#define OutputDir "..\publish"

[Setup]
AppId={{E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=Snap.Hutao.Remastered-{#MyAppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=no
SetupIconFile=..\src\Snap.Hutao.Remastered\Snap.Hutao.Remastered\Assets\Logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[CustomMessages]
; English (default)
CreateStartMenuIcon=Create a Start Menu shortcut(&S)
; Chinese Simplified
chinesesimplified.CreateStartMenuIcon=创建开始菜单快捷方式(&S)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "startmenuicon"; Description: "{cm:CreateStartMenuIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "VC_redist.x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Check: not IsVCInstalled
#ifdef CodeSigningCertificatePath
Source: "{#CodeSigningCertificatePath}"; DestDir: "{tmp}"; DestName: "{#CodeSigningCertificateFileName}"; Flags: ignoreversion deleteafterinstall
#endif

[Registry]
; hutao:// protocol registration
Root: HKLM; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "URL:{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""----AppNotificationActivated:"" ""%1"""; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AppUserModelId\Snap.Hutao.Remastered"; ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\AppUserModelId\Snap.Hutao.Remastered"; ValueType: string; ValueName: "IconUri"; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey


[Icons]
Name: "{group}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\{cm:UninstallProgram,{#MyAppShortName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppShortName}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  CodeSigningCertificateThumbprint = '{#CodeSigningCertificateThumbprint}';
  CodeSigningCertificateFileName = '{#CodeSigningCertificateFileName}';
  CertificateTrustMarkerKey = 'Software\SnapHutaoRemasteringProject\Snap.Hutao.Remastered';
  CertificateTrustMarkerValue = 'InstallerCreatedTrustedPeopleThumbprint';

function IsVCInstalled: Boolean;
var
  Key: string;
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  Key := 'SOFTWARE\Microsoft\VisualStudio\VC\Runtimes\AMD64';
  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE, Key, Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if Names[I] >= 'v14.0' then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

#ifdef CodeSigningCertificatePath
function CertificateExistsInTrustedPeople: Boolean;
begin
  Result := RegKeyExists(
    HKEY_LOCAL_MACHINE,
    'SOFTWARE\Microsoft\SystemCertificates\TrustedPeople\Certificates\' + CodeSigningCertificateThumbprint);
end;

procedure InstallCodeSigningCertificate;
var
  CertPath: string;
  ResultCode: Integer;
begin
  ResultCode := -1;
  CertPath := ExpandConstant('{tmp}\' + CodeSigningCertificateFileName);
  if not FileExists(CertPath) then
  begin
    Log('Code-signing certificate was not extracted. Skipping leaf trust installation.');
    Exit;
  end;

  if CertificateExistsInTrustedPeople then
  begin
    Log('Code-signing certificate already exists in TrustedPeople. Preserving its ownership state.');
    Exit;
  end;

  if Exec(
    'certutil.exe',
    '-addstore TrustedPeople "' + CertPath + '"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) and (ResultCode = 0) and CertificateExistsInTrustedPeople then
  begin
    RegWriteStringValue(
      HKEY_LOCAL_MACHINE,
      CertificateTrustMarkerKey,
      CertificateTrustMarkerValue,
      CodeSigningCertificateThumbprint);
    Log('Installed the code-signing certificate in TrustedPeople.');
  end
    else
  begin
    Log('Failed to install the code-signing certificate in TrustedPeople. Exit code: ' + IntToStr(ResultCode));
  end;
end;

procedure RemoveInstallerOwnedCodeSigningCertificate;
var
  InstalledThumbprint: string;
  ResultCode: Integer;
begin
  ResultCode := -1;
  if not RegQueryStringValue(
    HKEY_LOCAL_MACHINE,
    CertificateTrustMarkerKey,
    CertificateTrustMarkerValue,
    InstalledThumbprint) then
  begin
    Log('No installer-owned leaf trust marker found. Leaving TrustedPeople unchanged.');
    Exit;
  end;

  if CompareText(InstalledThumbprint, CodeSigningCertificateThumbprint) <> 0 then
  begin
    Log('Leaf trust marker does not match this installer. Leaving TrustedPeople unchanged.');
    Exit;
  end;

  if not CertificateExistsInTrustedPeople then
  begin
    RegDeleteValue(HKEY_LOCAL_MACHINE, CertificateTrustMarkerKey, CertificateTrustMarkerValue);
    Log('Installer-owned leaf trust was already absent. Removed its marker.');
    Exit;
  end;

  if Exec(
    'certutil.exe',
    '-delstore TrustedPeople ' + CodeSigningCertificateThumbprint,
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) and (ResultCode = 0) and not CertificateExistsInTrustedPeople then
  begin
    RegDeleteValue(HKEY_LOCAL_MACHINE, CertificateTrustMarkerKey, CertificateTrustMarkerValue);
    Log('Removed installer-owned code-signing certificate from TrustedPeople.');
  end
    else
  begin
    Log('Failed to remove installer-owned code-signing certificate from TrustedPeople. Exit code: ' + IntToStr(ResultCode));
  end;
end;
#endif

procedure CurStepChanged(CurStep: TSetupStep);
begin
#ifdef CodeSigningCertificatePath
  if CurStep = ssPostInstall then
  begin
    InstallCodeSigningCertificate;
  end;
#endif
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
#ifdef CodeSigningCertificatePath
  if CurUninstallStep = usUninstall then
  begin
    RemoveInstallerOwnedCodeSigningCertificate;
  end;
#endif

  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
  begin
    if MsgBox(
      '是否同时删除用户数据和缓存？' + #13#10 +
      'Do you also want to remove user data and cache?',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      DataDir := ExpandConstant('{localappdata}\SnapHutaoRemastered');
      if DirExists(DataDir) then
      begin
        if DelTree(DataDir, True, True, True) then
        begin
          Log('Successfully deleted: ' + DataDir);
        end;
      end;
    end;
  end;
end;

function InitializeSetup: Boolean;
var
  ResultCode: Integer;
  VcRedistPath: string;
begin
  Result := True;

  if not IsVCInstalled then
  begin
    VcRedistPath := ExpandConstant('{tmp}\VC_redist.x64.exe');
    if FileExists(VcRedistPath) then
    begin
      if MsgBox(
        'Visual C++ 运行时库未安装。是否立即安装？（必须安装才能运行此程序）' + #13#10 +
        'Visual C++ Redistributable is required. Install now?',
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        if not Exec(VcRedistPath, '/install /quiet /norestart', '', SW_SHOW,
          ewWaitUntilTerminated, ResultCode) then
        begin
          MsgBox('VC++ Redistributable 安装失败。请手动安装。' + #13#10 +
            'Failed to install VC++ Redistributable. Please install manually.',
            mbError, MB_OK);
        end;
      end;
    end;
  end;
end;
