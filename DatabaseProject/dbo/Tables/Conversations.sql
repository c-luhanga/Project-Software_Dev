CREATE TABLE [dbo].[Conversations] (
    [ConversationID] INT            IDENTITY (1, 1) NOT NULL,
    [LastMessage]    NVARCHAR (500) NULL,
    [LastUpdated]    DATETIME       DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ConversationID] ASC)
);

