$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..\..')).Path
$ProjectPath = Join-Path $RepoRoot 'Atad.UI\Atad.UI.csproj'
$PublishDir = Join-Path $RepoRoot 'artifacts\publish\windows-update'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Atad'
$MetadataPath = Join-Path $InstallDir 'install-metadata.json'
$SettingsPath = Join-Path $InstallDir 'settings.json'

if (-not (Test-Path $InstallDir) -or -not (Test-Path $MetadataPath)) {
	Write-Error 'No Atad installation was found for the current user. Run _install_windows.ps1 first.'
	exit 1
}

if (-not (Test-Path $ProjectPath)) {
	Write-Error "Project file not found: $ProjectPath"
	exit 1
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
	Write-Error 'git was not found in PATH. Install git and retry.'
	exit 1
}

$metadata = $null
try {
	$metadata = Get-Content -Path $MetadataPath -Raw | ConvertFrom-Json
}
catch {
	Write-Error 'Could not parse install metadata. Reinstall the application and retry.'
	exit 1
}

$commandName = 'atad'
$desktopShortcutPath = $null
if ($null -ne $metadata) {
	if (-not [string]::IsNullOrWhiteSpace($metadata.CommandName)) {
		$commandName = [string]$metadata.CommandName
	}

	if (-not [string]::IsNullOrWhiteSpace($metadata.DesktopShortcutPath)) {
		$desktopShortcutPath = [string]$metadata.DesktopShortcutPath
	}
}

Push-Location $RepoRoot
try {
	$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($branch)) {
		Write-Error 'Could not detect current git branch.'
		exit 1
	}

	& git fetch origin $branch
	if ($LASTEXITCODE -ne 0) {
		Write-Error 'git fetch failed.'
		exit 1
	}

	$localCommit = (& git rev-parse HEAD).Trim()
	$remoteCommit = (& git rev-parse "origin/$branch").Trim()
	if ($LASTEXITCODE -ne 0) {
		Write-Error 'Could not determine remote commit for update comparison.'
		exit 1
	}

	if ($localCommit -eq $remoteCommit) {
		Write-Host 'No updates found. Installation is already up to date.' -ForegroundColor Green
		exit 0
	}

	$statusBefore = (& git status --porcelain)
	if (-not [string]::IsNullOrWhiteSpace(($statusBefore | Out-String))) {
		Write-Error 'Working tree has local changes. Commit/stash them before running update.'
		exit 1
	}

	Write-Host "Pulling latest changes from origin/$branch ..."
	& git pull --ff-only origin $branch
	if ($LASTEXITCODE -ne 0) {
		Write-Error 'git pull failed.'
		exit 1
	}

	Write-Host 'Publishing updated app (self-contained, single-file)...'
	if (Test-Path $PublishDir) {
		Remove-Item -Path $PublishDir -Recurse -Force
	}

	& dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $PublishDir
	if ($LASTEXITCODE -ne 0) {
		Write-Error 'dotnet publish failed.'
		exit 1
	}

	$existingSettingsJson = $null
	if (Test-Path $SettingsPath) {
		$existingSettingsJson = Get-Content -Path $SettingsPath -Raw
	}

	if (Test-Path $InstallDir) {
		Get-ChildItem -Path $InstallDir -Force |
			Where-Object { $_.Name -ne 'settings.json' -and $_.Name -ne 'install-metadata.json' } |
			Remove-Item -Recurse -Force
	}

	Copy-Item -Path (Join-Path $PublishDir '*') -Destination $InstallDir -Recurse -Force

	if ($null -ne $existingSettingsJson) {
		Set-Content -Path $SettingsPath -Value $existingSettingsJson -Encoding UTF8
	}

	$exePath = Join-Path $InstallDir 'Atad.UI.exe'
	if (-not (Test-Path $exePath)) {
		$exe = Get-ChildItem -Path $InstallDir -Filter '*.exe' -File | Select-Object -First 1
		if ($null -eq $exe) {
			Write-Error 'Could not locate updated executable in install directory.'
			exit 1
		}

		$exePath = $exe.FullName
	}

	$shimPath = Join-Path $InstallDir "$commandName.cmd"
	$exeLeaf = Split-Path -Leaf $exePath
	$shimContent = "@echo off`r`n`"%~dp0$exeLeaf`" %*`r`n"
	Set-Content -Path $shimPath -Value $shimContent -Encoding ASCII

	if ($null -ne $desktopShortcutPath -and (Test-Path $desktopShortcutPath)) {
		$wsh = New-Object -ComObject WScript.Shell
		$shortcut = $wsh.CreateShortcut($desktopShortcutPath)
		$shortcut.TargetPath = $exePath
		$shortcut.WorkingDirectory = $InstallDir

		$iconPath = Join-Path $InstallDir 'assets\atad.ico'
		if (Test-Path $iconPath) {
			$shortcut.IconLocation = "$iconPath,0"
		}
		else {
			$shortcut.IconLocation = "$exePath,0"
		}

		$shortcut.Save()
	}

	$updatedMetadata = [ordered]@{
		InstallDirectory = $InstallDir
		ExecutablePath = $exePath
		CommandName = $commandName
		ShimPath = $shimPath
		DesktopShortcutPath = $desktopShortcutPath
		SettingsPath = $SettingsPath
		InstalledAt = if ($metadata.InstalledAt) { [string]$metadata.InstalledAt } else { (Get-Date).ToString('O') }
		UpdatedAt = (Get-Date).ToString('O')
	}
	$updatedMetadata | ConvertTo-Json | Set-Content -Path $MetadataPath -Encoding UTF8

	Write-Host ''
	Write-Host 'Update completed successfully.' -ForegroundColor Green
	Write-Host "Run the app from any terminal using: $commandName"
}
finally {
	Pop-Location
}
