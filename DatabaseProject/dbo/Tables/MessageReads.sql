CREATE TABLE [dbo].[MessageReads] (
    [MessageID] INT      NOT NULL,
    [UserID]    INT      NOT NULL,
    [ReadAt]    DATETIME DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_MessageReads] PRIMARY KEY CLUSTERED ([MessageID] ASC, [UserID] ASC),
    CONSTRAINT [FK_MessageReads_Messages] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Messages] ([MessageID]) ON DELETE CASCADE,
    CONSTRAINT [FK_MessageReads_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE
);

