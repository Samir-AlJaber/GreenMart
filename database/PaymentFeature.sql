USE GreenMartDB;
GO

IF COL_LENGTH('dbo.Orders', 'PaymentMethod') IS NULL
BEGIN
    ALTER TABLE dbo.Orders
        ADD PaymentMethod NVARCHAR(30) NOT NULL
            CONSTRAINT DF_Orders_PaymentMethod DEFAULT 'CashOnDelivery';
END;
GO

IF COL_LENGTH('dbo.Orders', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Orders
        ADD PaymentStatus NVARCHAR(30) NOT NULL
            CONSTRAINT DF_Orders_PaymentStatus DEFAULT 'CashOnDelivery';
END;
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        OrderId INT NOT NULL,
        TransactionId NVARCHAR(80) NOT NULL,
        GatewayTransactionId NVARCHAR(100) NULL,
        SessionKey NVARCHAR(100) NULL,
        Method NVARCHAR(30) NOT NULL,
        SelectedChannel NVARCHAR(30) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(3) NOT NULL CONSTRAINT DF_Payments_Currency DEFAULT 'BDT',
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Pending',
        ValidationId NVARCHAR(100) NULL,
        BankTransactionId NVARCHAR(80) NULL,
        CardType NVARCHAR(80) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSDATETIME(),
        PaidAt DATETIME2 NULL,
        CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId)
            REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
        CONSTRAINT UQ_Payments_OrderId UNIQUE (OrderId),
        CONSTRAINT UQ_Payments_TransactionId UNIQUE (TransactionId)
    );
END;
GO

-- Existing orders were created before online payment was available.
UPDATE dbo.Orders
SET PaymentMethod = 'CashOnDelivery',
    PaymentStatus = 'CashOnDelivery'
WHERE PaymentMethod IS NULL OR PaymentStatus IS NULL;
GO
