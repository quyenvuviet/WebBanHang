$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$backupPath = Join-Path $PSScriptRoot "BanHangcoiCrm.bak"
$dataPath = Join-Path $env:USERPROFILE "BanHangcoiCrm.mdf"
$logPath = Join-Path $env:USERPROFILE "BanHangcoiCrm_log.ldf"

if (-not (Test-Path -LiteralPath $backupPath)) {
    throw "Không tìm thấy file backup: $backupPath"
}

$sql = @"
USE master;
IF DB_ID(N'BanHangcoiCrm') IS NOT NULL
BEGIN
    ALTER DATABASE [BanHangcoiCrm] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [BanHangcoiCrm];
END;

RESTORE DATABASE [BanHangcoiCrm]
FROM DISK = N'$backupPath'
WITH
    MOVE N'BanHangcoiCrm' TO N'$dataPath',
    MOVE N'BanHangcoiCrm_log' TO N'$logPath',
    REPLACE,
    RECOVERY;
"@

sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -Q $sql
Write-Host "Đã restore database BanHangcoiCrm vào LocalDB."
