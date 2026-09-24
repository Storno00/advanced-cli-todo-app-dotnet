$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Atad'
$MetadataPath = Join-Path $InstallDir 'install-metadata.json'

if (-not (Test-Path $InstallDir) -or -not (Test-Path $MetadataPath)) {
	Write-Error 'No Atad installation was found for the current user. Nothing to uninstall.'
	exit 1
}

function Remove-UserPathEntry {
	param([Parameter(Mandatory = $true)][string]$Directory)

	$userPath = [Environment]::GetEnvironmentVariable('Path', [EnvironmentVariableTarget]::User)
	if ([string]::IsNullOrWhiteSpace($userPath)) {
		return
	}

	$entries = $userPath.Split(';', [StringSplitOptions]::RemoveEmptyEntries) |
		ForEach-Object { $_.Trim() } |
		Where-Object { $_ -ne $Directory }

	$updatedPath = ($entries -join ';')
	[Environment]::SetEnvironmentVariable('Path', $updatedPath, [EnvironmentVariableTarget]::User)

	$currentEntries = $env:Path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) |
		ForEach-Object { $_.Trim() } |
		Where-Object { $_ -ne $Directory }
	$env:Path = ($currentEntries -join ';')
}

$commandName = 'atad'
$desktopShortcutPath = $null
$shimPath = Join-Path $InstallDir "$commandName.cmd"

try {
	$metadata = Get-Content -Path $MetadataPath -Raw | ConvertFrom-Json

	if (-not [string]::IsNullOrWhiteSpace($metadata.CommandName)) {
		$commandName = [string]$metadata.CommandName
		$shimPath = Join-Path $InstallDir "$commandName.cmd"
	}

	if (-not [string]::IsNullOrWhiteSpace($metadata.ShimPath)) {
		$shimPath = [string]$metadata.ShimPath
	}

	if (-not [string]::IsNullOrWhiteSpace($metadata.DesktopShortcutPath)) {
		$desktopShortcutPath = [string]$metadata.DesktopShortcutPath
	}
}
catch {
	Write-Host 'Could not parse install metadata, continuing with default uninstall behavior.' -ForegroundColor Yellow
}

if ($null -ne $desktopShortcutPath -and (Test-Path $desktopShortcutPath)) {
	Remove-Item -Path $desktopShortcutPath -Force
	Write-Host "Removed desktop shortcut: $desktopShortcutPath"
}
else {
	$defaultShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) "$commandName.lnk"
	if (Test-Path $defaultShortcut) {
		Remove-Item -Path $defaultShortcut -Force
		Write-Host "Removed desktop shortcut: $defaultShortcut"
	}
}

if (Test-Path $shimPath) {
	Remove-Item -Path $shimPath -Force
	Write-Host "Removed command shim: $shimPath"
}

Remove-UserPathEntry -Directory $InstallDir

if (Test-Path $InstallDir) {
	Remove-Item -Path $InstallDir -Recurse -Force
	Write-Host "Removed install directory: $InstallDir"
}
else {
	Write-Host 'Install directory not found. Nothing to remove there.'
}

Write-Host ''
Write-Host 'Uninstall completed successfully.' -ForegroundColor Green
Write-Host 'MongoDB was not modified.'
