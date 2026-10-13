param([switch]$IfMissing)

$ErrorActionPreference = "Stop"
$instanceName = "BanHangcoi2025"
$server = "(localdb)\$instanceName"
$backupPath = Join-Path $PSScriptRoot "BanHangcoiCrm.bak"
$localDb = Join-Path $env:ProgramFiles "Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe"

if (-not (Test-Path -LiteralPath $backupPath)) {
    throw "Backup not found: $backupPath"
}
if (-not (Test-Path -LiteralPath $localDb)) {
    throw "Install SQL Server 2025 LocalDB (17.x). This backup cannot be restored on SQL Server 2012, 2016 or 2022."
}
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd was not found. Install the Microsoft SQL Server command-line tools."
}

$instances = & $localDb info 2>&1
if ($instances -notcontains $instanceName) {
    & $localDb create $instanceName 17.0
}
& $localDb start $instanceName
# LocalDB can report an error while returning exit code zero.
# Verify the SQL connection before doing any restore work.
$versionResult = & sqlcmd -S $server -E -C -b -l 15 -h -1 -W -Q "SET NOCOUNT ON; SELECT CONVERT(varchar(30), SERVERPROPERTY('ProductVersion'));"
if ($LASTEXITCODE -ne 0) { throw "Could not connect to SQL Server 2025 LocalDB: $server" }
$version = @($versionResult | Where-Object { $_ -match '^\d+\.\d+\.\d+' }) | Select-Object -First 1
if (-not $version -or [int]($version.Split('.')[0]) -lt 17) {
    throw "The backup requires SQL Server 2025 (17.x); connected version: $version"
}

$existing = & sqlcmd -S $server -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'BanHangcoiCrm') IS NULL THEN 0 ELSE 1 END;"
if ($LASTEXITCODE -ne 0) { throw "Failed to check whether the database exists." }
if (@($existing | Where-Object { $_.Trim() -eq '1' }).Count -gt 0) {
    if ($IfMissing) {
        Write-Host "Database already exists on $server; keeping existing data."
        return
    }
    throw "BanHangcoiCrm already exists on $server. Existing data will not be overwritten."
}

$dataDirectory = Join-Path $PSScriptRoot "local"
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
$dataPath = Join-Path $dataDirectory "BanHangcoiCrm.mdf"
$logPath = Join-Path $dataDirectory "BanHangcoiCrm_log.ldf"
if ((Test-Path -LiteralPath $dataPath) -or (Test-Path -LiteralPath $logPath)) {
    throw "Database files already exist in $dataDirectory. Restore stopped to preserve them."
}
foreach ($path in @($backupPath, $dataPath, $logPath)) {
    if ($path.Contains("'")) { throw "SQLCMD restore paths must not contain single quotes: $path" }
}
& sqlcmd -S $server -E -C -b -d master -i (Join-Path $PSScriptRoot "restore_localdb.sql") -v "BackupPath=$backupPath" "DataPath=$dataPath" "LogPath=$logPath"
if ($LASTEXITCODE -ne 0) { throw "Database restore failed. Check the SQL Server error above." }
Write-Host "Restored BanHangcoiCrm on $server (SQL Server $version)."
