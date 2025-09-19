CREATE TABLE [dbo].[ItemImages] (
    [ImageID]  INT            IDENTITY (1, 1) NOT NULL,
    [ItemID]   INT            NOT NULL,
    [ImageURL] NVARCHAR (500) NOT NULL,
    PRIMARY KEY CLUSTERED ([ImageID] ASC),
    CONSTRAINT [FK_ItemImages_Items] FOREIGN KEY ([ItemID]) REFERENCES [dbo].[Items] ([ItemID]) ON DELETE CASCADE
);

