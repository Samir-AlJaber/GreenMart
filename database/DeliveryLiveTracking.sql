USE GreenMartDB;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'CurrentLatitude') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD CurrentLatitude DECIMAL(9,6) NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'CurrentLongitude') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD CurrentLongitude DECIMAL(9,6) NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'LastLocationUpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD LastLocationUpdatedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'IsLocationSharing') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.DeliveryAssignments
        ADD IsLocationSharing BIT NOT NULL
        CONSTRAINT DF_DeliveryAssignments_IsLocationSharing DEFAULT (0)');
END;
GO
