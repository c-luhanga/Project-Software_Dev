CREATE TABLE [dbo].[Users] (
    [UserID]          INT            IDENTITY (1, 1) NOT NULL,
    [FirebaseUID]     NVARCHAR (128) NULL,
    [FirstName]       NVARCHAR (100) NOT NULL,
    [LastName]        NVARCHAR (100) NOT NULL,
    [Email]           NVARCHAR (255) NOT NULL,
    [PasswordHash]    NVARCHAR (MAX) NOT NULL,
    [Phone]           NVARCHAR (20)  NULL,
    [House]           NVARCHAR (50)  NULL,
    [IsBanned]        BIT            DEFAULT ((0)) NOT NULL,
    [IsAdmin]         BIT            DEFAULT ((0)) NOT NULL,
    [IsDeleted]       BIT            DEFAULT ((0)) NOT NULL,
    [CreatedAt]       DATETIME       DEFAULT (getdate()) NOT NULL,
    [LastSeen]        DATETIME       NULL,
    [ProfileImageURL] NVARCHAR (500) NULL,
    PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [CK_Users_Email_Principia] CHECK (right(lower([Email]),len('@principia.edu'))='@principia.edu')
);


GO
CREATE NONCLUSTERED INDEX [IX_Users_Email]
    ON [dbo].[Users]([Email] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Users_Email_Active]
    ON [dbo].[Users]([Email] ASC) WHERE ([IsDeleted]=(0));


GO
CREATE NONCLUSTERED INDEX [IX_Users_LastSeen]
    ON [dbo].[Users]([LastSeen] ASC);

