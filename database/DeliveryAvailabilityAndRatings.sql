USE GreenMartDB;
GO

IF COL_LENGTH('dbo.DeliveryManApplications', 'IsAvailable') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.DeliveryManApplications
        ADD IsAvailable BIT NOT NULL
            CONSTRAINT DF_DeliveryManApplications_IsAvailable DEFAULT (0)');
END;
GO

UPDATE dbo.DeliveryManApplications
SET IsAvailable = 1
WHERE Status = 'Approved'
  AND IsAvailable = 0
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.DeliveryAssignments assignment
      WHERE assignment.DeliveryManId = DeliveryManApplications.UserId
        AND assignment.Status <> 'Delivered'
  );
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'SellerId') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.DeliveryAssignments ADD SellerId INT NULL');
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DeliveryAssignments') AND name = 'SellerId' AND is_nullable = 1)
BEGIN
    UPDATE assignment
    SET SellerId = seller.UserId
    FROM dbo.DeliveryAssignments assignment
    CROSS APPLY
    (
        SELECT TOP (1) product.UserId
        FROM dbo.OrderItems item
        INNER JOIN dbo.Products product ON product.ProductId = item.ProductId
        WHERE item.OrderId = assignment.OrderId
        ORDER BY item.OrderItemId
    ) seller;

    IF EXISTS (SELECT 1 FROM dbo.DeliveryAssignments WHERE SellerId IS NULL)
        THROW 51000, 'An existing delivery assignment has no matching seller.', 1;

    ALTER TABLE dbo.DeliveryAssignments ALTER COLUMN SellerId INT NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DeliveryAssignment_Seller')
BEGIN
    ALTER TABLE dbo.DeliveryAssignments
        ADD CONSTRAINT FK_DeliveryAssignment_Seller
            FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId);
END;
GO

IF COL_LENGTH('dbo.DeliveryAssignments', 'DeliveredAt') IS NULL
BEGIN
    ALTER TABLE dbo.DeliveryAssignments ADD DeliveredAt DATETIME2 NULL;
END;
GO

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_DeliveryAssignments_OrderId'
      AND object_id = OBJECT_ID('dbo.DeliveryAssignments')
)
BEGIN
    DROP INDEX UX_DeliveryAssignments_OrderId ON dbo.DeliveryAssignments;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_DeliveryAssignments_OrderId_SellerId'
      AND object_id = OBJECT_ID('dbo.DeliveryAssignments')
)
BEGIN
    CREATE UNIQUE INDEX UX_DeliveryAssignments_OrderId_SellerId
        ON dbo.DeliveryAssignments(OrderId, SellerId);
END;
GO

IF OBJECT_ID('dbo.DeliveryRatings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryRatings
    (
        DeliveryRatingId INT IDENTITY(1,1) PRIMARY KEY,
        DeliveryAssignmentId INT NOT NULL,
        CustomerId INT NOT NULL,
        DeliveryManId INT NOT NULL,
        RatingValue INT NOT NULL,
        RatingLabel VARCHAR(20) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

        CONSTRAINT CK_DeliveryRatings_Value CHECK (RatingValue BETWEEN 1 AND 5),
        CONSTRAINT FK_DeliveryRating_Assignment FOREIGN KEY (DeliveryAssignmentId)
            REFERENCES dbo.DeliveryAssignments(DeliveryAssignmentId),
        CONSTRAINT FK_DeliveryRating_Customer FOREIGN KEY (CustomerId)
            REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_DeliveryRating_DeliveryMan FOREIGN KEY (DeliveryManId)
            REFERENCES dbo.Users(UserId)
    );

    CREATE UNIQUE INDEX UX_DeliveryRatings_AssignmentId
        ON dbo.DeliveryRatings(DeliveryAssignmentId);
END;
GO
