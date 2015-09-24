<#
.SYNOPSIS
Installs the Microsoft Git Credential Manager for Windows.

.DESCRIPTION
Installs the Microsoft Git Credential Manager for Windows. Attempts to locate and update any-and-all Git for Windows installations on the local system by looking in common paths.

Supports:

 * Custom installation paths
 * Custom Git for Windows installation paths
 * Skipping Microsoft .NET Framework detection

Requires:

 * Git for Windows 1.9.5 or later.
 * Windows Vista or later.
 * Microsoft .NET Framework v4.5.1 or later.

.PARAMETER PathToGit
Specifies a location to look for git.exe when installing in addition to any Git installation locations detected.

.PARAMETER InstallTo
Specifies a path to install to. This is in addition to any Git installation locations detected.

.PARAMETER SkipNetfx
Specifies that the installer should skip the detection of the Microsoft .NET Framwork (aka netfx) and that the installer should progress regardless.
#>
[CmdletBinding()]
Param(
    [alias("--git-path")]
    [string]$PathToGit,
    [alias("--install-to")]
    [string]$InstallTo,
    [alias("--skip-netfx")]
    [switch]$SkipNetfx
)

## Globals

$title = "Microsoft Git Credential Manager for Windows"
$name="manager"

$installPath = Split-Path $MyInvocation.MyCommand.Path
$netxfxMin = New-Object System.Version("4.5.1")

## Helper functions

function NetFxInstalled($pathPart) {
<#
    .DESCRIPTION
    Checks if a .NET Framework greater than version 4.5.1 is installed
#>
    $netfx4Path = "HKLM:\Software\Microsoft\Net Framework Setup\NDP\v4\$pathPart"

    if (Test-Path $netfx4Path) {
        $detectedVersion = New-Object System.Version((Get-ItemProperty $netfx4Path | Select-Object -ExpandProperty Version))
        return ($detectedVersion.CompareTo($netxfxMin) -ge 0)
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
        Write-Output ""
        Write-Output "Deploying from $installPath to $destination"
        Write-Output ""

        # Copy all dlls and exes.
        $err = @()
        $files = @("Microsoft.Alm.Authentication.dll", 
                   "Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
                   "Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll"
                   "git-credential-manager.exe")
        
        foreach ($file in $files) {
            Copy-Item "$installPath\$file" $destination -Force -ErrorAction SilentlyContinue -ErrorVariable +err
            Write-Output " $file"
        }

        # Check if any copies failed. If one did, abort the entire script.
        if ($err.Count -gt 0) {
            Write-Error ""
            Write-Error "Errors encountered when copying files."

            foreach ($e in $err) {
                Write-Error $e
            }

            # Exit with error code 4 (file copy error)
            exit 4
        }

        # Update this git's config.
        & "$localGitPath\git.exe" config --global credential.helper $name
        if ($LastExitCode -eq 0) {
            Write-Output ""
            Write-Output "Updated your ~/.gitconfig [git config --global]"
        }
    }
}

## Welcome message

Write-Output "Hello! I'll install the $title."

## Validate parameters

if (-not [String]::IsNullOrWhiteSpace($InstallTo)) {
    if (-not (Test-Path $InstallTo)) {
        Write-Warning ""
        Write-Warning "Cannot install to custom path. Path does not exist: '$InstallTo'"
        $InstallTo = ""
    } else {
        Write-Verbose ""
        Write-Verbose "Installing to custom path: $InstallTo"
    }
}

if (-not [String]::IsNullOrWhiteSpace($PathToGit)) {
    $gitExePath = "$PathToGit\Cmd"

    if (-not (Test-Path "$gitExePath\git.exe")) {
        Write-Warning ""
        Write-Warning "Git.exe not found in custom path '$PathToGit'"        
        $PathToGit = ""
    } else {
        Write-Verbose ""
        Write-Verbose "Git.exe found in custom path: '$gitExePath'"
    }
}

## Validate administrator access

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error ""
    Write-Error "The script must be run as Administrator. Start Windows PowerShell by using the Run as Administrator options, and try running again."

    # Exit with error code 2 (insufficient privileges)
    exit 2
}

## Detect NetFx 4.5.1 or greater.

if (-not $SkipNetfx) {
    if (-not (NetFxInstalled "Client" -or NetFxInstalled "Full")) {
        Write-Error ""
        Write-Error "Failed to detect the Microsoft .NET Framework. Make sure it is installed. U_U"
        Write-Error "Don't know where to get the Microsoft .NET Framework? Try http://bit.ly/1kE08Rz"

        # Exit with error code 6 (minimum NetFX support not found)
        exit 4
    }
} else {
    Write-Verbose "Microsoft .NET Framework detection skipped."
}

# Installation

if (-not [String]::IsNullOrWhiteSpace($InstallTo)) {
    Write-Output "Deploying to custom location: $InstallTo"
    PerformSetup $InstallTo
}

$pfNativeDestination = "$Env:ProgramFiles\Git\Cmd"
$pfWowDestination = "${Env:ProgramFiles(x86)}\Git\Cmd"

$candidatePaths = @{}

if (-not [String]::IsNullOrWhiteSpace($PathToGit)) {
    $customGitDestination = "$PathToGit\Git\Cmd"

    $candidatePaths + @{
        "$PathToGit\mingw64\libexec\git-core\" = $customGitDestination; # 64-bit Git for Windows 2.x
        "$PathToGit\mingw32\libexec\git-core\" = $customGitDestination; # 32-bit Git for Windows 2.x
        "$PathToGit\libexec\git-core\" = $customGitDestination;         # 32-bit Git for Windows 1.x
        }
}

$candidatePaths = $candidatePaths + @{
    "$Env:ProgramFiles\Git\mingw64\libexec\git-core" = $pfNativeDestination     # 64-bit Git for Windows 2.x on 64-bit Windows
    "$Env:ProgramFiles\Git\mingw32\libexec\git-core" = $pfNativeDestination     # 32-bit Git for Windows 2.x on 32-bit Windows
    "${Env:ProgramFiles(x86)}\Git\mingw32\libexec\git-core" = $pfWowDestination # 32-bit Git for Windows 2.x on 64-bit Windows
    "$Env:ProgramFiles\Git\libexec\git-core" = $pfNativeDestination             # 32-bit Git for Windows 1.x on 32-bit Windows
    "${Env:ProgramFiles(X86)}\Git\libexec\git-core" = $pfWowDestination         # 32-bit Git for Windows 1.x on 64-bit Windows
}

Write-Output ""
Write-Output "Looking for Git installation(s)..."

Foreach ($paths in $candidatePaths.GetEnumerator()) {
    PerformSetup $paths
}

Write-Output ""
Write-Output "Success! $title was installed! ^_^"
Write-Output ""

# Exit with error code 0 (success)
exit 0
