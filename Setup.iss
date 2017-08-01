; This script requires Inno Setup Compiler 5.5.9 or later to compile
; The Inno Setup Compiler (and IDE) can be found at http://www.jrsoftware.org/isinfo.php
; The IDP plugin for Inno Setup is also required and can be found at https://mitrichsoftware.wordpress.com/inno-setup-tools/inno-download-plugin/

#if VER < EncodeVer(5,5,9)
  #error Update your Inno Setup version (5.5.9 or newer)
#endif

#define deployDir "Deploy\"

#ifnexist deployDir + "git-credential-manager.exe"
  #error Compile Git Credential Manager first
#endif

#include <idp.iss>

#define VerMajor
#define VerMinor
#define VerBuild
#define VerRevision

#expr ParseVersion(deployDir + "git-credential-manager.exe", VerMajor, VerMinor, VerBuild, VerRevision)

#define MyAppName "Microsoft Git Credential Manager for Windows"
#define MyAppVersion str(VerMajor) + "." + str(VerMinor) + "." + str(VerBuild)
#define MyAppPublisher "Microsoft Corporation"
#define MyAppPublisherURL "https://www.microsoft.com"
#define MyAppURL "https://github.com/Microsoft/Git-Credential-Manager-for-Windows"
#define MyAppExeName "git-credential-manager.exe"
#define Git4WinVer "2.13.3"
#define Git4WinVerLong = "v" + str(Git4WinVer) + ".windows.1"
#define Git4WinName "Git for Windows " + str(Git4WinVer)
#define Git4WinFile "Git-" + str(Git4WinVer) + "-64-bit.exe"
#define Git4WinUrl "https://github.com/git-for-windows/git/releases/download/" + str(Git4WinVerLong) + "/" + str(Git4WinFile)
#define Git4WinSpace 394309632
#define NetFxName "The Microsoft .NET Framework 4.7"
#define NetFxBaseFile "NetFx40Installer.exe"
#define NetFxBaseUrl "https://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe"
#define NetFxCoreFile "NetFx46Installer.exe"
#define NetFxCoreUrl "https://download.microsoft.com/download/A/E/A/AEAE0F3F-96E9-4711-AADA-5E35EF902306/NDP47-KB3186500-Web.exe"
#define NetFxSpace 381005824

[Setup]
AppId={{9F0CBE43-690B-4C03-8845-6AC2CDB29815}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppPublisherURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppURL}
AppCopyright=Copyright © Microsoft 2017
VersionInfoVersion={#MyAppVersion}
AppReadmeFile=https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/README.md
BackColor=clWhite
BackSolid=yes
DefaultDirName={userpf}\{#MyAppName}
LicenseFile={#deployDir}\LICENSE.txt
OutputBaseFilename=GCMW-{#MyAppVersion}
Compression=lzma2
InternalCompressLevel=ultra64
SolidCompression=yes
MinVersion=6.1.7600
DisableDirPage=yes
DisableReadyPage=yes
UninstallDisplayIcon={app}\git-credential-manager.exe
SetupIconFile=Assets\gcmicon.ico
ArchitecturesInstallIn64BitMode=x64
WizardImageFile=Assets\gcmicon128.bmp
WizardSmallImageFile=Assets\gcmicon64.bmp
WizardImageStretch=no
WindowResizable=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl";

[Types]
Name: "full"; Description: "Full installation"; Flags: iscustom;

[Components]
Name: "NetFx"; Description: {#NetFxName}; ExtraDiskSpaceRequired: {#NetFxSpace}; Types: full; Flags: fixed; Check: DetectNetFxChecked;
Name: "Git4Win"; Description: {#Git4WinName}; ExtraDiskSpaceRequired: {#Git4WinSpace}; Types: full; Flags: fixed; Check: DetectGitChecked;

[Files]
Source: "{#deployDir}\git-credential-manager.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Microsoft.Alm.Authentication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Microsoft.Alm.Git.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Microsoft.IdentityModel.Clients.ActiveDirectory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Microsoft.IdentityModel.Clients.ActiveDirectory.Platform.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Microsoft.Vsts.Authentication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\GitHub.Authentication.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\Bitbucket.Authentication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\git-credential-manager.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#deployDir}\git-askpass.html"; DestDir: "{app}"; Flags: ignoreversion

[Code]
type NetFx_Version = (
   NetFx_v30,  // .NET Framework 3.0
   NetFx_v35,  // .NET Framework 3.5
   NetFx_v40,  // .NET Framework 4.0
   NetFx_v45,  // .NET Framework 4.5
   NetFx_v451, // .NET Framework 4.5.1
   NetFx_v452, // .NET Framework 4.5.2
   NetFx_v46,  // .NET Framework 4.6
   NetFx_v461, // .NET Framework 4.6.1
   NetFx_v462, // .NET Framework 4.6.2
   NetFx_v47   // .NET Framework 4.7
);

function DetectGit(): Boolean;
var
  bSuccess: Boolean;
  strValue: String;
begin
  Result := True;

  bSuccess := RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1', 'InstallLocation', strValue)
           or RegQueryStringValue(HKLM, 'SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1', 'InstallLocation', strValue)
           or RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1', 'InstallLocation', strValue)
           or RegQueryStringValue(HKCU, 'SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1', 'InstallLocation', strValue);

  if not bSuccess then
    begin
      Result := False;
    end;
end;

function DetectGitChecked(): Boolean;
begin
  Result := not DetectGit();
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

      if (NetFx_v461 = version) and (regVersion >= 394254) then
        begin
          Result := True;
        end;

      if (NetFx_v462 = version) and (regVersion >= 394802) then
        begin
          Result := True;
        end;

      if (NetFx_V47 = version) and (regVersion >= 460798) then
        begin
          Result := True;
        end;
    end;
end;

function DetectNetFxChecked(): Boolean;
begin
  Result := not DetectNetFx(NetFx_v451);
end;

function InstallPrerequisites() : Boolean;
var
  bInstallFx40: Boolean;
  bInstallFx46: Boolean;
  bInstallGit: Boolean;
  StatusText: String;
  ResultCode: Integer;
begin
  Result := True;

  bInstallFx40 := FileExists(ExpandConstant('{tmp}\{#NetFxBaseFile}'));
  bInstallFx46 := FileExists(ExpandConstant('{tmp}\{#NetFxCoreFile}'));
  bInstallGit := FileExists(ExpandConstant('{tmp}\{#Git4WinFile}'));

  if bInstallFx40 or bInstallFx46 then
    begin
      StatusText := WizardForm.StatusLabel.Caption;
      WizardForm.StatusLabel.Caption := 'Installing {#NetFxName}. This might take a few minutes...';
      WizardForm.ProgressGauge.Style := npbstMarquee;

      try
        if bInstallFx40 then
          begin
            if not Exec(ExpandConstant('{tmp}\{#NetFxBaseFile}'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
              begin
                Result := False;
                MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
              end;
          end;

        if bInstallFx46 then
          begin
            if not Exec(ExpandConstant('{tmp}\{#NetFxCoreFile}'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
              begin
                Result := False;
                MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
              end;
          end;
      finally
        WizardForm.StatusLabel.Caption := StatusText;
        WizardForm.ProgressGauge.Style := npbstNormal;
      end;
    end;

  if bInstallGit then
    begin
      StatusText := WizardForm.StatusLabel.Caption;
      WizardForm.StatusLabel.Caption := 'Installing {#Git4WinName}. This might take a few minutes...';
      WizardForm.ProgressGauge.Style := npbstMarquee;

      try
        if not Exec(ExpandConstant('{tmp}\{#Git4WinFile}'), '/NOCANCEL', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
          begin
            Result := False;
            MsgBox('Installing Git for Windows failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
          end;
      finally
        WizardForm.StatusLabel.Caption := StatusText;
        WizardForm.ProgressGauge.Style := npbstNormal;
      end;
    end;
end;

function InstallManager() : Boolean;
var
  ResultCode: Integer;
  StatusText: String;
begin
  Result := False;

  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Installing {#MyAppName}.';
  WizardForm.ProgressGauge.Style := npbstMarquee;

  try
    if Exec(ExpandConstant('{app}\git-credential-manager.exe'), 'deploy --passive --nofail', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        Result := True;
      end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

function UninstallManager() : Boolean;
var
  ResultCode: Integer;
  StatusText: String;
begin
  Result := false;

  if Exec(ExpandConstant('{app}\git-credential-manager.exe'), 'remove --passive --nofail', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := True;
    end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard;
begin
  if not DetectNetFx(NetFx_v40) then
    begin
      idpAddFile('{#NetFxBaseUrl}', ExpandConstant('{tmp}\{#NetFxBaseFile}'));
      idpDownloadAfter(wpReady);
    end;

  if not DetectNetFx(NetFx_v451) then
    begin
      idpAddFile('{#NetFxCoreUrl}', ExpandConstant('{tmp}\{#NetFxCoreFile}'));
      idpDownloadAfter(wpReady);
    end;

  if not DetectGit() then
    begin
      idpAddFile('{#Git4WinUrl}', ExpandConstant('{tmp}\{#Git4WinFile}'));
      idpDownloadAfter(wpReady);
    end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpLicense then
    begin
      WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall);
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  case CurStep of
    ssInstall:
      begin
        if not (InstallPrerequisites()) then
          begin
            Abort();
          end;
      end;

    ssPostInstall:
      begin
        if not (InstallManager()) then
          begin
            RaiseException('Fatal: An error occured when updating the local system.');
          end;
      end;
  end;
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
begin
  case CurStep of
    usUninstall:
      if not (UninstallManager()) then
        begin
          Abort();
        end;
    end;
end;
