CREATE TABLE [dbo].[Messages] (
    [MessageID]      INT            IDENTITY (1, 1) NOT NULL,
    [ConversationID] INT            NOT NULL,
    [SenderID]       INT            NOT NULL,
    [Content]        NVARCHAR (MAX) NOT NULL,
    [Timestamp]      DATETIME       DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([MessageID] ASC),
    CONSTRAINT [FK_Messages_Conversations] FOREIGN KEY ([ConversationID]) REFERENCES [dbo].[Conversations] ([ConversationID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Users] FOREIGN KEY ([SenderID]) REFERENCES [dbo].[Users] ([UserID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Messages_Conversation_Time]
    ON [dbo].[Messages]([ConversationID] ASC, [Timestamp] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_Messages_SenderID_Time]
    ON [dbo].[Messages]([SenderID] ASC, [Timestamp] DESC);

