USE GreenMartDB;
GO

CREATE TABLE Users
(
    UserId INT PRIMARY KEY IDENTITY(1,1),
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(150) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    Address VARCHAR(255),
    Role VARCHAR(20) DEFAULT 'User',
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);


CREATE TABLE Categories
(
    CategoryId INT PRIMARY KEY IDENTITY(1,1),

    CategoryName VARCHAR(100) NOT NULL,

    Description VARCHAR(500),

    IsActive BIT DEFAULT 1,

    CreatedAt DATETIME DEFAULT GETDATE()
);
GO



CREATE TABLE Products
(
    ProductId INT PRIMARY KEY IDENTITY(1,1),

    ProductName VARCHAR(150) NOT NULL,

    Brand VARCHAR(100),

    Description VARCHAR(500),

    Price DECIMAL(10,2) NOT NULL,

    StockQuantity INT DEFAULT 0,

    IsActive BIT DEFAULT 1,

    CreatedAt DATETIME DEFAULT GETDATE(),


    UserId INT NOT NULL,

    CategoryId INT NOT NULL,


    CONSTRAINT FK_Product_User
    FOREIGN KEY(UserId)
    REFERENCES Users(UserId),


    CONSTRAINT FK_Product_Category
    FOREIGN KEY(CategoryId)
    REFERENCES Categories(CategoryId)

);
GO





CREATE TABLE Carts
(
    CartId INT PRIMARY KEY IDENTITY(1,1),

    UserId INT UNIQUE NOT NULL,

    CreatedAt DATETIME DEFAULT GETDATE(),


    CONSTRAINT FK_Cart_User
    FOREIGN KEY(UserId)
    REFERENCES Users(UserId)

);
GO





CREATE TABLE CartItems
(
    CartItemId INT PRIMARY KEY IDENTITY(1,1),

    CartId INT NOT NULL,

    ProductId INT NOT NULL,

    Quantity INT DEFAULT 1,

    AddedAt DATETIME DEFAULT GETDATE(),



    CONSTRAINT FK_CartItem_Cart
    FOREIGN KEY(CartId)
    REFERENCES Carts(CartId),



    CONSTRAINT FK_CartItem_Product
    FOREIGN KEY(ProductId)
    REFERENCES Products(ProductId)

);
GO





CREATE TABLE Orders
(
    OrderId INT PRIMARY KEY IDENTITY(1,1),

    UserId INT NOT NULL,

    TotalAmount DECIMAL(10,2) NOT NULL,

    Status VARCHAR(50) DEFAULT 'Pending',

    ShippingAddress VARCHAR(500),

    CreatedAt DATETIME DEFAULT GETDATE(),



    CONSTRAINT FK_Order_User
    FOREIGN KEY(UserId)
    REFERENCES Users(UserId)

);
GO






CREATE TABLE OrderItems
(
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),

    OrderId INT NOT NULL,

    ProductId INT NOT NULL,

    Quantity INT NOT NULL,

    Price DECIMAL(10,2) NOT NULL,

    CreatedAt DATETIME DEFAULT GETDATE(),



    CONSTRAINT FK_OrderItem_Order
    FOREIGN KEY(OrderId)
    REFERENCES Orders(OrderId),



    CONSTRAINT FK_OrderItem_Product
    FOREIGN KEY(ProductId)
    REFERENCES Products(ProductId)

);
GO






CREATE TABLE Reviews
(
    ReviewId INT PRIMARY KEY IDENTITY(1,1),

    UserId INT NOT NULL,

    ProductId INT NOT NULL,

    Rating INT NOT NULL,

    Comment VARCHAR(500),

    CreatedAt DATETIME DEFAULT GETDATE(),



    CONSTRAINT FK_Review_User
    FOREIGN KEY(UserId)
    REFERENCES Users(UserId),



    CONSTRAINT FK_Review_Product
    FOREIGN KEY(ProductId)
    REFERENCES Products(ProductId)

);
GO



CREATE TABLE ExchangeRequests
(
    ExchangeRequestId INT PRIMARY KEY IDENTITY(1,1),


    RequestedProductId INT NOT NULL,


    OfferedProductId INT NOT NULL,


    RequesterId INT NOT NULL,


    OwnerId INT NOT NULL,


    Status VARCHAR(50) DEFAULT 'Pending',


    Message VARCHAR(500),


    CreatedAt DATETIME DEFAULT GETDATE(),



    CONSTRAINT FK_Exchange_Request_Product
    FOREIGN KEY(RequestedProductId)
    REFERENCES Products(ProductId),



    CONSTRAINT FK_Exchange_Offer_Product
    FOREIGN KEY(OfferedProductId)
    REFERENCES Products(ProductId),



    CONSTRAINT FK_Exchange_Requester
    FOREIGN KEY(RequesterId)
    REFERENCES Users(UserId),



    CONSTRAINT FK_Exchange_Owner
    FOREIGN KEY(OwnerId)
    REFERENCES Users(UserId)

);
GO






CREATE TABLE AdminLogs
(
    LogId INT PRIMARY KEY IDENTITY(1,1),
    AdminId INT NOT NULL,
    Action VARCHAR(200) NOT NULL,
    TargetUserId INT NULL,
    TargetProductId INT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_AdminLog_Admin
    FOREIGN KEY(AdminId)
    REFERENCES Users(UserId),

    CONSTRAINT FK_AdminLog_User
    FOREIGN KEY(TargetUserId)
    REFERENCES Users(UserId),



    CONSTRAINT FK_AdminLog_Product
    FOREIGN KEY(TargetProductId)
    REFERENCES Products(ProductId)

);
GO

INSERT INTO Categories
(CategoryName, Description)
VALUES

('Electronics', 'Phones, computers, gadgets and electronic items'),

('Fashion', 'Clothing, shoes and accessories'),

('Furniture', 'Home and office furniture'),

('Books', 'Books and educational materials'),

('Food', 'Food and grocery products'),

('Vehicles', 'Cars, bikes and vehicle related items'),

('Others', 'Other products');

GO