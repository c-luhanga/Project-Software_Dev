CREATE TABLE [dbo].[Categories] (
    [CategoryID] INT           IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([CategoryID] ASC),
    UNIQUE NONCLUSTERED ([Name] ASC)
);

