; This script requires Inno Setup Compiler 5.5.6 or later to compile

#include <idp.iss>

#define MyAppName "Git Credential Manager for Windows"
#define MyAppVersion "0.9.14"
#define MyAppPublisher "Microsoft Corporation"
#define MyAppURL "https://github.com/Microsoft/Git-Credential-Manager-for-Windows"
#define MyAppExeName "git-credential-manager.exe"

[Setup]
AppId={{9F0CBE43-690B-4C03-8845-6AC2CDB29815}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
BackSolid=yes
DefaultDirName={userpf}\{#MyAppName}
LicenseFile=..\Deploy\LICENSE.TXT
OutputBaseFilename=Setup
Compression=lzma2/ultra64
SolidCompression=yes
MinVersion=6.1.7600
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableReadyPage=yes
SetupIconFile=Assets\gcmicon.ico
ArchitecturesInstallIn64BitMode=x64
WizardImageBackColor=clWhite
WizardImageFile=Assets\gcmicon128.bmp
WizardSmallImageFile=Assets\gcmicon64.bmp
WizardImageStretch=no
WindowResizable=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{tmp}\gcmSetup"

[Files]
Source: "..\Deploy\git-credential-manager.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Deploy\Microsoft.Alm.Authentication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Deploy\Microsoft.Alm.Git.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Deploy\Microsoft.IdentityModel.Clients.ActiveDirectory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Deploy\Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Deploy\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Code]
type NetFx_Version = (
     NetFx_v30,  // .NET Framework 3.0
     NetFx_v35,  // .NET Framework 3.5
     NetFx_v40,  // .NET Framework 4.0
     NetFx_v45,  // .NET Framework 4.5
     NetFx_v451, // .NET Framework 4.5.1
     NetFx_v452, // .NET Framework 4.5.2
     NetFx_v46); // .NET Framework 4.6

function DetectGit(): Boolean;
var
  bSuccess: Boolean;
  strValue: String;
begin
    Result := False;

    bSuccess := RegQueryStringValue(HKLM, 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1', 'InstallLocation', strValue);
    if not bSuccess then
      begin
        Result := False;
        MsgBox('Git for Windows was not detected in the system.', mbError, MB_OK);
      end;
end;

function DetectNetFx(version: NetFx_Version): Boolean;
var
  bSuccess: Boolean;
  regVersion: Cardinal;
begin
  Result := False;

  bSuccess := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.0', 'Install', regVersion);

  if (NetFx_v30 = version) and bSuccess then
    begin
      Result := True;
    end;

  bSuccess := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5', 'Install', regVersion);

  if (NetFx_v35 = version) and bSuccess then
    begin
      Result := True;
    end;

  bSuccess := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4.0\Full', 'Install', regVersion)
           or RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4.0\Client', 'Install', regVersion);

  if (NetFx_v40 = version) and bSuccess then
      begin
        Result := True;
      end;

  bSuccess := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', regVersion)
           or RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client', 'Release', regVersion);

  if bSuccess then
    begin
      if (NetFx_v45 = version) and (regVersion >= 378389)then
        begin
          Result := True;
        end;

      if (NetFx_v451 = version) and (regVersion >= 378675)then
        begin
          Result := True;
        end;

      if (NetFx_v452 = version) and (regVersion >= 379893)then
        begin
          Result := True;
        end;

      if (NetFx_v46 = version) and (regVersion >= 393295) then
        begin
          Result := True;
        end;
    end;
end;

function InstallFramework() : Boolean;
var
  StatusText: string;
  ResultCode: Integer;
begin
  Result := True;

  if FileExists(ExpandConstant('{tmp}\NetFx40Installer.exe')) then
    begin
      StatusText := WizardForm.StatusLabel.Caption;
      WizardForm.StatusLabel.Caption := 'Installing .NET Framework. This might take a few minutes...';
      WizardForm.ProgressGauge.Style := npbstMarquee;
      try
        if not Exec(ExpandConstant('{tmp}\NetFx40Installer.exe'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
          begin
            Result := False;
            MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
          end;
      finally
        WizardForm.StatusLabel.Caption := StatusText;
        WizardForm.ProgressGauge.Style := npbstNormal;

        DeleteFile(ExpandConstant('{tmp}\NetFx40Installer.exe'));
      end;
    end;

  if FileExists(ExpandConstant('{tmp}\NetFx46Installer.exe')) then
    begin
      StatusText := WizardForm.StatusLabel.Caption;
      WizardForm.StatusLabel.Caption := 'Installing .NET Framework. This might take a few minutes...';
      WizardForm.ProgressGauge.Style := npbstMarquee;
      try
        if not Exec(ExpandConstant('{tmp}\NetFx46Installer.exe'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
          begin
            Result := False;
            MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
          end;
      finally
        WizardForm.StatusLabel.Caption := StatusText;
        WizardForm.ProgressGauge.Style := npbstNormal;

        DeleteFile(ExpandConstant('{tmp}\NetFx46Installer.exe'));
      end;
    end;
end;

function InstallManager() : Boolean;
var
  ResultCode: integer;
  StatusText: string;
begin
  Result := False;

  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Installing Git Credential Manager for Windows.';
  WizardForm.ProgressGauge.Style := npbstMarquee;

  try
    if Exec(ExpandConstant('{app}\git-credential-manager.exe'), 'install --passive --nofail', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        Result := True;
      end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;     
end;

procedure InitializeWizard;
begin
  if not DetectNetFx(NetFx_v40) then
    begin
      idpAddFile('http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe', ExpandConstant('{tmp}\NetFx40Installer.exe'));
      idpDownloadAfter(wpReady);
    end;

  if not DetectNetFx(NetFx_v451) then
    begin
      idpAddFile('http://download.microsoft.com/download/1/4/A/14A6C422-0D3C-4811-A31F-5EF91A83C368/NDP46-KB3045560-Web.exe', ExpandConstant('{tmp}\NetFx46Installer.exe'));
      idpDownloadAfter(wpReady);
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  case CurStep of
    ssInstall:
      begin
        if not (InstallFramework() and DetectGit()) then
          begin
            Abort();
          end;
      end;

    ssPostInstall:
      begin
        if not (InstallManager()) then
          begin
            Abort();
          end;
      end;
  end;  
end;

