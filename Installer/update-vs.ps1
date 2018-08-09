$installer_name = 'git-credential-manager.exe'
$title = 'Git Credential Manager for Windows update utility for Visual Studio 2017 v1.0'

function check_access {
  # Ensure that we're running elevated, and if we're not then restart as elevated.
  $current_identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
  $current_principle = new-object System.Security.Principal.WindowsPrincipal($current_identity)
  $administrator_role = [System.Security.Principal.WindowsBuiltInRole]::Administrator
  
  if (-not ($current_principle.IsInRole($administrator_role))) {
    write-host -BackgroundColor Black -ForegroundColor Yellow 'Administrator privileges are required. Please re-start PowerShell as administrator and re-run this script'
    
    exit -2
  }
}

$current_directory = get-location
$installer_path = "$current_directory\$installer_name"
$vs_installer_path = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"

write-host -BackgroundColor Black -ForegroundColor Cyan $title

if (-not (test-path($installer_path))) {
  write-host -BackgroundColor Black -ForegroundColor Red $text "Failed to find '$installer_name', quitting."
  
  exit -1
}

if (-not (test-path($vs_installer_path))) {
  write-host -BackgroundColor Black -ForegroundColor Yellow  'Visual Studio was not detected on this machine.'
  
  exit 1
}

# Ensure that this script is executing elevated
check_access

write-host 'Scanning for installations of Visual Studio to update'
write-host 

$vswhere_path = "$vs_installer_path\vswhere.exe"
$vswhere_cmd = "& '$vswhere_path' -all -prerelease -nologo -version '[15.0, 16.0)'"

# Run vswhere and parse its output.
$vswhere_output = (iex "$vswhere_cmd | findstr /I 'installation'") | out-string
$reader = new-object -TypeName System.IO.StringReader -ArgumentList $vswhere_output
[int]$success_count = 0
[int]$failure_count = 0
$installation_name
$installation_path

while ($line = $reader.ReadLine()) {
  if ($line -like 'installationName: *') {
    $installation_name = $line.SubString('installationName: '.Length)
  } elseif ($line -like 'installationPath: *') {
    $installation_path = $line.SubString('installationPath: '.Length)
    
    write-host -BackgroundColor Black -ForegroundColor Cyan "Found $installation_name ($installation_path)"
    
    $installation_path  = "$installation_path\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git"
    
    # Execute the command line installer script.
    $installer_cmd = "& '$installer_path' install --passive --path '$installation_path'"
    iex "$installer_cmd"
    
    if ($lastExitCode -ne 0) {
      $failure_count = $failure_count + 1
      write-host -BackgroundColor Black -ForegroundColor Yellow "Failed to update '$installation_path'"
    } else {
      $success_count = $success_count + 1
    }
    
    write-host 
  }
}

[int]$total_count = $failure_count + $success_count

if ($total_count -gt 0) {
  $message = "$total_count Visual Studio installation(s) found"
  if ($success_count -gt 0) {
    $message = "$message, $success_count successfully updated"
  }
  if ($failure_count -gt 0) {
    $message = "$message, $failure_count failed to updated"
  }
  $message = "$message."
  
  write-host -BackgroundColor Black -ForegroundColor Cyan $message
} else {
  write-host 'Nothing to update.'
}
