USE GreenMartDB;
GO

IF OBJECT_ID('dbo.DeliveryManApplications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryManApplications
    (
        DeliveryManApplicationId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        NidNumber VARCHAR(30) NOT NULL,
        VehicleType VARCHAR(40) NOT NULL,
        VehicleNumber VARCHAR(50) NOT NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        SubmittedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        ReviewedAt DATETIME2 NULL,
        ReviewedByUserId INT NULL,
        IsAvailable BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_DeliveryManApplication_User
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),

        CONSTRAINT FK_DeliveryManApplication_Reviewer
            FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE UNIQUE INDEX UX_DeliveryManApplications_UserId
        ON dbo.DeliveryManApplications(UserId);
END;
GO

IF OBJECT_ID('dbo.DeliveryAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryAssignments
    (
        DeliveryAssignmentId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        DeliveryManId INT NOT NULL,
        SellerId INT NOT NULL,
        PickupAddress NVARCHAR(300) NOT NULL,
        PickupPhone VARCHAR(20) NOT NULL,
        PickupInstructions NVARCHAR(500) NULL,
        Status VARCHAR(30) NOT NULL DEFAULT 'Assigned',
        AssignedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        PickupStartedAt DATETIME2 NULL,
        ArrivedAtPickupAt DATETIME2 NULL,
        PickedUpAt DATETIME2 NULL,
        CurrentLatitude DECIMAL(9,6) NULL,
        CurrentLongitude DECIMAL(9,6) NULL,
        LastLocationUpdatedAt DATETIME2 NULL,
        IsLocationSharing BIT NOT NULL DEFAULT 0,
        DeliveredAt DATETIME2 NULL,

        CONSTRAINT FK_DeliveryAssignment_Order
            FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),

        CONSTRAINT FK_DeliveryAssignment_DeliveryMan
            FOREIGN KEY (DeliveryManId) REFERENCES dbo.Users(UserId),

        CONSTRAINT FK_DeliveryAssignment_Seller
            FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId)
    );

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

-- After choosing an existing GreenMart account as the administrator,
-- replace the email below and run the UPDATE once in SSMS:
-- UPDATE dbo.Users SET Role = 'Admin' WHERE Email = 'admin@gmail.com';
