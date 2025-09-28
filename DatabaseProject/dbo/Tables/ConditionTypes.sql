CREATE TABLE [dbo].[ConditionTypes] (
    [ConditionID] TINYINT       NOT NULL,
    [Name]        NVARCHAR (20) NOT NULL,
    PRIMARY KEY CLUSTERED ([ConditionID] ASC),
    UNIQUE NONCLUSTERED ([Name] ASC)
);

