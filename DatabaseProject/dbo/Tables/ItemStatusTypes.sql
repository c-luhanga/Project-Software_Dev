CREATE TABLE [dbo].[ItemStatusTypes] (
    [StatusID] TINYINT       NOT NULL,
    [Name]     NVARCHAR (20) NOT NULL,
    PRIMARY KEY CLUSTERED ([StatusID] ASC),
    UNIQUE NONCLUSTERED ([Name] ASC)
);

