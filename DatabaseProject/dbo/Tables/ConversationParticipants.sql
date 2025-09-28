CREATE TABLE [dbo].[ConversationParticipants] (
    [ConversationID] INT NOT NULL,
    [UserID]         INT NOT NULL,
    CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY CLUSTERED ([ConversationID] ASC, [UserID] ASC),
    CONSTRAINT [FK_ConvPart_Conversations] FOREIGN KEY ([ConversationID]) REFERENCES [dbo].[Conversations] ([ConversationID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ConvPart_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ConvPart_User_Conv]
    ON [dbo].[ConversationParticipants]([UserID] ASC, [ConversationID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ConversationParticipants_UserID]
    ON [dbo].[ConversationParticipants]([UserID] ASC);

