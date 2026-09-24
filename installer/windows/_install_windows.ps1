$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..\..')).Path
$ProjectPath = Join-Path $RepoRoot 'Atad.UI\Atad.UI.csproj'
$PublishDir = Join-Path $RepoRoot 'artifacts\publish\windows'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Atad'
$AssetsDir = Join-Path $InstallDir 'assets'
$SettingsPath = Join-Path $InstallDir 'settings.json'
$MetadataPath = Join-Path $InstallDir 'install-metadata.json'

$DefaultConnectionString = 'mongodb://localhost:27017'
$DefaultDatabaseName = 'AtadTodoDb'
$DefaultCommandName = 'atad'

function Read-InputOrDefault {
	param(
		[Parameter(Mandatory = $true)][string]$Prompt,
		[Parameter(Mandatory = $true)][string]$Default
	)

	$value = Read-Host "$Prompt [$Default]"
	if ([string]::IsNullOrWhiteSpace($value)) {
		return $Default
	}

	return $value.Trim()
}

function Read-YesNo {
	param(
		[Parameter(Mandatory = $true)][string]$Prompt,
		[bool]$Default = $true
	)

	$defaultText = if ($Default) { 'Y/n' } else { 'y/N' }

	while ($true) {
		$value = Read-Host "$Prompt ($defaultText)"

		if ([string]::IsNullOrWhiteSpace($value)) {
			return $Default
		}

		switch ($value.Trim().ToLowerInvariant()) {
			'y' { return $true }
			'yes' { return $true }
			'n' { return $false }
			'no' { return $false }
			default { Write-Host 'Please answer yes or no.' -ForegroundColor Yellow }
		}
	}
}

function Install-MongoDb {
	if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
		throw 'winget is required to install MongoDB automatically, but it was not found on this system.'
	}

	$packageIds = @(
		'MongoDB.Server',
		'MongoDB.MongoDBCommunityServer'
	)

	foreach ($packageId in $packageIds) {
		Write-Host "Attempting MongoDB installation via winget package '$packageId'..."
		& winget install --id $packageId --exact --accept-package-agreements --accept-source-agreements --disable-interactivity

		if ($LASTEXITCODE -eq 0) {
			Write-Host 'MongoDB installed successfully.' -ForegroundColor Green
			return
		}
	}

	throw 'MongoDB installation via winget failed. Please install MongoDB manually and rerun the installer.'
}

function Get-MongoHostPort {
	param([Parameter(Mandatory = $true)][string]$ConnectionString)

	if ($ConnectionString -notmatch '^mongodb://') {
		throw 'Only mongodb:// connection strings are supported by the installer validation step.'
	}

	$withoutScheme = $ConnectionString.Substring('mongodb://'.Length)
	$authority = $withoutScheme.Split('/')[0]
	$firstHost = $authority.Split(',')[0]

	if ($firstHost.Contains('@')) {
		$firstHost = $firstHost.Split('@')[1]
	}

	if ($firstHost.Contains(':')) {
		$parts = $firstHost.Split(':')
		return @{ Host = $parts[0]; Port = [int]$parts[1] }
	}

	return @{ Host = $firstHost; Port = 27017 }
}

function Test-MongoConnection {
	param([Parameter(Mandatory = $true)][string]$ConnectionString)

	try {
		$endpoint = Get-MongoHostPort -ConnectionString $ConnectionString
		return Test-NetConnection -ComputerName $endpoint.Host -Port $endpoint.Port -InformationLevel Quiet
	}
	catch {
		Write-Host $_.Exception.Message -ForegroundColor Yellow
		return $false
	}
}

function Ensure-UserPathContains {
	param([Parameter(Mandatory = $true)][string]$Directory)

	$userPath = [Environment]::GetEnvironmentVariable('Path', [EnvironmentVariableTarget]::User)
	if ([string]::IsNullOrWhiteSpace($userPath)) {
		$userPath = ''
	}

	$entries = $userPath.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() }
	if ($entries -contains $Directory) {
		return
	}

	$updatedPath = if ([string]::IsNullOrWhiteSpace($userPath)) { $Directory } else { "$userPath;$Directory" }
	[Environment]::SetEnvironmentVariable('Path', $updatedPath, [EnvironmentVariableTarget]::User)

	if (-not ($env:Path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) -contains $Directory)) {
		$env:Path = "$env:Path;$Directory"
	}
}

function Get-IconSourcePath {
	param([Parameter(Mandatory = $true)][string]$Root)

	$candidates = @(
		(Join-Path $Root 'installer\windows\assets\atad.ico'),
		(Join-Path $Root '_assets\atad.ico')
	)

	foreach ($candidate in $candidates) {
		if (Test-Path $candidate) {
			return $candidate
		}
	}

	$fallback = Get-ChildItem -Path $Root -Filter '*.ico' -File -Recurse | Select-Object -First 1
	if ($null -ne $fallback) {
		return $fallback.FullName
	}

	return $null
}

if (-not (Test-Path $ProjectPath)) {
	throw "Project file not found: $ProjectPath"
}

Write-Host 'Publishing app (self-contained, single-file)...'
if (Test-Path $PublishDir) {
	Remove-Item -Path $PublishDir -Recurse -Force
}

& dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $PublishDir
if ($LASTEXITCODE -ne 0) {
	throw 'dotnet publish failed.'
}

Write-Host "Installing files to $InstallDir ..."
if (Test-Path $InstallDir) {
	Remove-Item -Path $InstallDir -Recurse -Force
}

New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $InstallDir -Recurse -Force

$exePath = Join-Path $InstallDir 'Atad.UI.exe'
if (-not (Test-Path $exePath)) {
	$exe = Get-ChildItem -Path $InstallDir -Filter '*.exe' -File | Select-Object -First 1
	if ($null -eq $exe) {
		throw 'Could not locate installed executable.'
	}

	$exePath = $exe.FullName
}

$iconSourcePath = Get-IconSourcePath -Root $RepoRoot
$iconPath = $null
if ($null -ne $iconSourcePath) {
	New-Item -Path $AssetsDir -ItemType Directory -Force | Out-Null
	$iconPath = Join-Path $AssetsDir 'atad.ico'
	Copy-Item -Path $iconSourcePath -Destination $iconPath -Force
}

$hasMongo = Read-YesNo -Prompt 'Do you already have MongoDB installed?' -Default $true
if (-not $hasMongo) {
	Install-MongoDb
}

$connectionString = if ($hasMongo) {
	Read-InputOrDefault -Prompt 'MongoDB connection string' -Default $DefaultConnectionString
}
else {
	$DefaultConnectionString
}

while (-not (Test-MongoConnection -ConnectionString $connectionString)) {
	Write-Host 'MongoDB connection test failed. Please provide another connection string.' -ForegroundColor Yellow
	$connectionString = Read-InputOrDefault -Prompt 'MongoDB connection string' -Default $DefaultConnectionString
}

$dbName = if ($hasMongo) {
	Read-InputOrDefault -Prompt 'MongoDB database name' -Default $DefaultDatabaseName
}
else {
	$DefaultDatabaseName
}

$commandName = Read-InputOrDefault -Prompt 'CLI command to launch the app' -Default $DefaultCommandName
while ($commandName -notmatch '^[a-zA-Z0-9_-]+$') {
	Write-Host 'Command may contain only letters, numbers, underscore, and hyphen.' -ForegroundColor Yellow
	$commandName = Read-InputOrDefault -Prompt 'CLI command to launch the app' -Default $DefaultCommandName
}

$shimPath = Join-Path $InstallDir "$commandName.cmd"
$exeLeaf = Split-Path -Leaf $exePath
$shimContent = "@echo off`r`n`"%~dp0$exeLeaf`" %*`r`n"
Set-Content -Path $shimPath -Value $shimContent -Encoding ASCII

Ensure-UserPathContains -Directory $InstallDir

$desktopShortcutPath = $null
if (Read-YesNo -Prompt 'Create desktop shortcut?' -Default $false) {
	$desktopPath = [Environment]::GetFolderPath('Desktop')
	$desktopShortcutPath = Join-Path $desktopPath "$commandName.lnk"

	$wsh = New-Object -ComObject WScript.Shell
	$shortcut = $wsh.CreateShortcut($desktopShortcutPath)
	$shortcut.TargetPath = $exePath
	$shortcut.WorkingDirectory = $InstallDir

	if ($null -ne $iconPath -and (Test-Path $iconPath)) {
		$shortcut.IconLocation = "$iconPath,0"
	}
	else {
		$shortcut.IconLocation = "$exePath,0"
	}

	$shortcut.Save()
}

$settings = [ordered]@{
	ConnectionString = $connectionString
	DatabaseName = $dbName
}
$settings | ConvertTo-Json | Set-Content -Path $SettingsPath -Encoding UTF8

$metadata = [ordered]@{
	InstallDirectory = $InstallDir
	ExecutablePath = $exePath
	CommandName = $commandName
	ShimPath = $shimPath
	DesktopShortcutPath = $desktopShortcutPath
	SettingsPath = $SettingsPath
	InstalledAt = (Get-Date).ToString('O')
}
$metadata | ConvertTo-Json | Set-Content -Path $MetadataPath -Encoding UTF8

Write-Host ''
Write-Host 'Installation completed successfully.' -ForegroundColor Green
Write-Host "Run the app from any terminal using: $commandName"
Write-Host "Installed directory: $InstallDir"
