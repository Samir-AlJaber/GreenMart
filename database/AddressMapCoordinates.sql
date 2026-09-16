USE GreenMartDB;
GO

IF COL_LENGTH('dbo.Orders', 'ShippingLatitude') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD ShippingLatitude DECIMAL(9,6) NULL;
END;
GO

IF COL_LENGTH('dbo.Orders', 'ShippingLongitude') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD ShippingLongitude DECIMAL(9,6) NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupLatitude') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD PickupLatitude DECIMAL(9,6) NULL;
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'PickupLongitude') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD PickupLongitude DECIMAL(9,6) NULL;
END;
GO
