CREATE TABLE [dbo].[Items] (
    [ItemID]      INT             IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (255)  NOT NULL,
    [Description] NVARCHAR (MAX)  NOT NULL,
    [Category]    NVARCHAR (50)   NOT NULL,
    [Price]       DECIMAL (10, 2) NULL,
    [Condition]   NVARCHAR (20)   NOT NULL,
    [SellerID]    INT             NOT NULL,
    [PostedDate]  DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ItemID] ASC),
    CONSTRAINT [FK_Items_Users] FOREIGN KEY ([SellerID]) REFERENCES [dbo].[Users] ([UserID])
);

