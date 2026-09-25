USE GreenMartDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SellerPayoutAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SellerPayoutAccounts
    (
        SellerPayoutAccountId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SellerPayoutAccounts PRIMARY KEY,
        SellerId INT NOT NULL,
        Method NVARCHAR(20) NOT NULL,
        AccountHolderName NVARCHAR(120) NOT NULL,
        AccountNumber NVARCHAR(40) NOT NULL,
        BankName NVARCHAR(120) NULL,
        BranchName NVARCHAR(120) NULL,
        RoutingNumber NVARCHAR(30) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SellerPayoutAccounts_Status DEFAULT 'PendingVerification',
        IsActive BIT NOT NULL CONSTRAINT DF_SellerPayoutAccounts_IsActive DEFAULT 1,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SellerPayoutAccounts_UpdatedAt DEFAULT SYSDATETIME(),
        VerifiedAt DATETIME2 NULL,
        VerifiedByUserId INT NULL,
        CONSTRAINT FK_SellerPayoutAccounts_Seller FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_SellerPayoutAccounts_Seller UNIQUE (SellerId)
    );
END;
GO

IF OBJECT_ID('dbo.SellerEarnings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SellerEarnings
    (
        SellerEarningId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SellerEarnings PRIMARY KEY,
        SellerId INT NOT NULL,
        OrderId INT NOT NULL,
        DeliveryAssignmentId INT NOT NULL,
        GrossAmount DECIMAL(18,2) NOT NULL,
        GatewayFee DECIMAL(18,2) NOT NULL CONSTRAINT DF_SellerEarnings_GatewayFee DEFAULT 0,
        CommissionAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SellerEarnings_Commission DEFAULT 0,
        NetAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(30) NOT NULL,
        Status NVARCHAR(40) NOT NULL,
        StatusNote NVARCHAR(300) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SellerEarnings_CreatedAt DEFAULT SYSDATETIME(),
        AvailableAt DATETIME2 NULL,
        PaidAt DATETIME2 NULL,
        CONSTRAINT FK_SellerEarnings_Seller FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_SellerEarnings_Order FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
        CONSTRAINT FK_SellerEarnings_Assignment FOREIGN KEY (DeliveryAssignmentId) REFERENCES dbo.DeliveryAssignments(DeliveryAssignmentId),
        CONSTRAINT UQ_SellerEarnings_Assignment UNIQUE (DeliveryAssignmentId)
    );
END;
GO

IF OBJECT_ID('dbo.SellerPayouts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SellerPayouts
    (
        SellerPayoutId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SellerPayouts PRIMARY KEY,
        SellerEarningId INT NOT NULL,
        SellerId INT NOT NULL,
        Provider NVARCHAR(30) NOT NULL,
        AccountNumberSnapshot NVARCHAR(40) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_SellerPayouts_Status DEFAULT 'AwaitingProviderSetup',
        ExternalReference NVARCHAR(100) NULL,
        FailureReason NVARCHAR(300) NULL,
        RequestedAt DATETIME2 NOT NULL CONSTRAINT DF_SellerPayouts_RequestedAt DEFAULT SYSDATETIME(),
        ProcessedAt DATETIME2 NULL,
        CONSTRAINT FK_SellerPayouts_Earning FOREIGN KEY (SellerEarningId)
            REFERENCES dbo.SellerEarnings(SellerEarningId) ON DELETE CASCADE,
        CONSTRAINT FK_SellerPayouts_Seller FOREIGN KEY (SellerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_SellerPayouts_Earning UNIQUE (SellerEarningId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.SellerPayouts')
      AND name = 'UX_SellerPayouts_ExternalReference'
)
BEGIN
    CREATE UNIQUE INDEX UX_SellerPayouts_ExternalReference
        ON dbo.SellerPayouts(ExternalReference)
        WHERE ExternalReference IS NOT NULL;
END;
GO
