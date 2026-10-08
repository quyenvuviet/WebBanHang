USE master;
GO

IF DB_ID(N'BanHangcoiCrm') IS NOT NULL
BEGIN
    ALTER DATABASE [BanHangcoiCrm] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [BanHangcoiCrm];
END
GO

RESTORE DATABASE [BanHangcoiCrm]
FROM DISK = N'D:\AI_AGENCY_AGENTS\WebBanHang\database\BanHangcoiCrm.bak'
WITH
    MOVE N'BanHangcoiCrm' TO N'C:\Users\PC\BanHangcoiCrm.mdf',
    MOVE N'BanHangcoiCrm_log' TO N'C:\Users\PC\BanHangcoiCrm_log.ldf',
    REPLACE,
    RECOVERY;
GO
