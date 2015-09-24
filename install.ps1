<#
.SYNOPSIS
Installs the Git Credential Manager for Windows.

.PARAMETER gitPath
Specifies a location to look for git.exe when installing in addition to any Git installation locations detected.

.PARAMETER installTo
Specifies a path to install to. This is in addition to any Git installation locations detected.

.PARAMETER noNetfx
Specifies that the installer should NOT detect if the Microsoft .NET Framwork (aka NetFX) is installed or not and that the installer should progress regardless.
#>
[CmdletBinding()]
Param(
    [string]$GitPath,
    [string]$InstallTo,
    [switch]$NoNetfx
)

## Globals.

$gitExtensionName = "Microsoft Git Credential Manager for Windows"
$name="manager"
$exeName = "git-credential-$name.exe"

$installPath = Split-Path $MyInvocation.MyCommand.Path
$minimumVersion = New-Object System.Version("4.5.1")

## Helper functions

function NetFxInstalled($pathPart) {
<#
    .DESCRIPTION
    Checks if a .NET Framework greater than version 4.5.1 is installed
#>
    $netfx4Path = "HKLM:\Software\Microsoft\Net Framework Setup\NDP\v4\$pathPart"

    if (Test-Path $netfx4Path) {
        $detectedVersion = New-Object System.Version((Get-ItemProperty $netfx4Path | Select-Object -ExpandProperty Version))
        return ($detectedVersion.CompareTo($minimumVersion) -ge 0)
    }

    return $False
}

function PerformSetup($paths) {
<#
    .DESCRIPTION
    Deploys the credential helper to the destination path.
#>
    $destination = $paths.Key

    # If the path exists, deploy into it.
    if (Test-Path $destination) {

        $localGitPath = $paths.Value
        Write-Verbose "Deploying from $installPath to $destination"

        # Copy all dlls and exes.
        $err = @()
        Copy-Item "$installPath\*.dll" $destination -Force -ErrorAction SilentlyContinue -ErrorVariable +err
        Copy-Item "$installPath\*.exe" $destination -Force -ErrorAction SilentlyContinue -ErrorVariable +err

        # Check if any copies failed. If one did, abort the entire script.
        if ($err.Length -gt 0) {
            Write-Warning "Errors encountered when copying files."
            foreach ($e in $err) {
                Write-Warning $e
            }
            throw "Couldn't copy files."
        }

        # Update this git's config.
        & "$localGitPath\git.exe" config --global credential.helper $name
        if ($LastExitCode -eq 0) {
            Write-Host "Updated your ~/.gitconfig [git config --global]"
        }
    }
}

## Validate parameters

if ($InstallTo.Length) {
    if (-not (Test-Path $InstallTo)) {
        Write-Warning "Cannot install to custom path. Path does not exist: $InstallTo"
        throw "Invalid Parameter"
    } else {
        Write-Verbose "Installing to custom path: $InstallTo"
    }
}

if ($GitPath.Length) {
    $gitExePath = "$GitPath\cmd"
    if (-not (Test-Path "$gitExePath\git.exe")) {
        Write-Warning "Git.exe not found in custom path $GitPath"
        throw "Invalid Parameter"
    } else {
        Write-Verbose "Git.exe found in custom path: $gitExePath"
    }
}

## Validate administrator access

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "The script must be run as Administrator. Start Windows PowerShell by using the Run as Administrator options, and try running again."
    throw "Need admin permissions"
}

## Detect NetFx 4.5.1 or greater.

if (-not $NoNetfx) {
    if (-not (NetFxInstalled "Client" -or NetFxInstalled "Full")) {
        Write-Warning "Failed to detect the Microsoft .NET Framework. Make sure it is installed. U_U"
        Write-Warning "Don't know where to get the Microsoft .NET Framework? Try http://bit.ly/1kE08Rz"
        throw "Failed to find prerequisite."

    }
}
else {
    Write-Verbose "NetFX detection skipped."
}

# Installation

if ($InstallTo.Length) {
    Write-Verbose "Deploying to custom location: $InstallTo"
    PerformSetup $InstallTo
}

$customGitDestination = "$GitPath\Git\Cmd"
$pfNativeDestination = "$Env:ProgramFiles\Git\Cmd"
$pfWowDestination = "${Env:ProgramFiles(x86)}\Git\Cmd"

$candidatePaths = @{}

if ($GitPath.Length) {
    $candidatePaths = @{
        "$GitPath\mingw64\libexec\git-core\" = $customGitDestination; # 64-bit Git for Windows 2.x
        "$GitPath\mingw32\libexec\git-core\" = $customGitDestination; # 32-bit Git for Windows 2.x
        "$GitPath\libexec\git-core\" = $customGitDestination;         # 32-bit Git for Windows 1.x
        }
}

$candidatePaths = $candidatePaths + @{
    "$Env:ProgramFiles\Git\mingw64\libexec\git-core" = $pfNativeDestination     # 64-bit Git for Windows 2.x on 64-bit Windows
    "$Env:ProgramFiles\Git\mingw32\libexec\git-core" = $pfNativeDestination     # 32-bit Git for Windows 2.x on 32-bit Windows
    "${Env:ProgramFiles(x86)}\Git\mingw32\libexec\git-core" = $pfWowDestination # 32-bit Git for Windows 2.x on 64-bit Windows
    "$Env:ProgramFiles\Git\libexec\git-core" = $pfNativeDestination             # 32-bit Git for Windows 1.x on 32-bit Windows
    "${Env:ProgramFiles(X86)}\Git\libexec\git-core" = $pfWowDestination         # 32-bit Git for Windows 1.x on 64-bit Windows
}

Write-Verbose "Looking for Git installation(s)..."

Foreach ($paths in $candidatePaths.GetEnumerator()) {
    PerformSetup $paths
}

Write-Host "Success! $gitExtensionName was installed! ^^_^^"