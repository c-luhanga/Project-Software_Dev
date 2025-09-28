CREATE TABLE [dbo].[Conversations] (
    [ConversationID] INT            IDENTITY (1, 1) NOT NULL,
    [ItemID]         INT            NULL,
    [LastMessage]    NVARCHAR (500) NULL,
    [LastUpdated]    DATETIME       DEFAULT (getdate()) NOT NULL,
    [CreatedAt]      DATETIME       DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ConversationID] ASC),
    CONSTRAINT [FK_Conversations_Items] FOREIGN KEY ([ItemID]) REFERENCES [dbo].[Items] ([ItemID]) ON DELETE SET NULL
);


GO
CREATE NONCLUSTERED INDEX [IX_Conversations_Item_Updated]
    ON [dbo].[Conversations]([ItemID] ASC, [LastUpdated] DESC);

