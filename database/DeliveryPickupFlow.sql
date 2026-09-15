USE GreenMartDB;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupAddress') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.DeliveryAssignments
        ADD PickupAddress NVARCHAR(300) NOT NULL
        CONSTRAINT DF_DeliveryAssignments_PickupAddress DEFAULT ('''')');
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupPhone') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.DeliveryAssignments
        ADD PickupPhone VARCHAR(20) NOT NULL
        CONSTRAINT DF_DeliveryAssignments_PickupPhone DEFAULT ('''')');
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupInstructions') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD PickupInstructions NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupStartedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD PickupStartedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'ArrivedAtPickupAt') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD ArrivedAtPickupAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickedUpAt') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD PickedUpAt DATETIME2 NULL;
END;
GO

UPDATE assignment
SET PickupAddress = COALESCE(NULLIF(assignment.PickupAddress, ''), seller.Address, ''),
    PickupPhone = COALESCE(NULLIF(assignment.PickupPhone, ''), seller.PhoneNumber, '')
FROM dbo.DeliveryAssignments assignment
INNER JOIN dbo.Users seller ON seller.UserId = assignment.SellerId
WHERE assignment.PickupAddress = '' OR assignment.PickupPhone = '';
GO
