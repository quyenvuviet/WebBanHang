:ON ERROR EXIT
USE master;
GO
IF CONVERT(int, PARSENAME(CONVERT(varchar(30), SERVERPROPERTY('ProductVersion')), 4)) < 17
    THROW 50001, 'This backup requires SQL Server 2025 (17.x) or later.', 1;
IF DB_ID(N'BanHangcoiCrm') IS NOT NULL
    THROW 50002, 'BanHangcoiCrm already exists. Restore stopped to preserve existing data.', 1;
GO
RESTORE VERIFYONLY FROM DISK = N'$(BackupPath)';
GO
RESTORE DATABASE [BanHangcoiCrm]
FROM DISK = N'$(BackupPath)'
WITH
    MOVE N'BanHangcoiCrm' TO N'$(DataPath)',
    MOVE N'BanHangcoiCrm_log' TO N'$(LogPath)',
    RECOVERY;
GO
