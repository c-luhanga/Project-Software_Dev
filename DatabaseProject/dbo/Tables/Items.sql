CREATE TABLE [dbo].[Items] (
    [ItemID]      INT             IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (255)  NOT NULL,
    [Description] NVARCHAR (MAX)  NOT NULL,
    [CategoryID]  INT             NULL,
    [Price]       DECIMAL (10, 2) NULL,
    [ConditionID] TINYINT         NOT NULL,
    [StatusID]    TINYINT         DEFAULT ((1)) NOT NULL,
    [SellerID]    INT             NOT NULL,
    [PostedDate]  DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ItemID] ASC),
    CONSTRAINT [CK_Items_Price_NonNegative] CHECK ([Price] IS NULL OR [Price]>=(0)),
    CONSTRAINT [FK_Items_Category] FOREIGN KEY ([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID]),
    CONSTRAINT [FK_Items_Condition] FOREIGN KEY ([ConditionID]) REFERENCES [dbo].[ConditionTypes] ([ConditionID]),
    CONSTRAINT [FK_Items_Status] FOREIGN KEY ([StatusID]) REFERENCES [dbo].[ItemStatusTypes] ([StatusID]),
    CONSTRAINT [FK_Items_Users] FOREIGN KEY ([SellerID]) REFERENCES [dbo].[Users] ([UserID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Items_CategoryID]
    ON [dbo].[Items]([CategoryID] ASC) WHERE ([CategoryID] IS NOT NULL);


GO
CREATE NONCLUSTERED INDEX [IX_Items_ConditionID]
    ON [dbo].[Items]([ConditionID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Items_Status]
    ON [dbo].[Items]([StatusID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Items_Seller_PostedDate]
    ON [dbo].[Items]([SellerID] ASC, [PostedDate] DESC);

