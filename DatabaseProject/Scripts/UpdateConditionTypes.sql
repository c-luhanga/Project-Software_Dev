-- Update ConditionTypes to match frontend expectations
-- Frontend expects: 1=New, 2=Like New, 3=Good, 4=Fair, 5=Poor

-- First, update existing records to avoid conflicts
BEGIN TRANSACTION;

-- Temporarily disable constraints
ALTER TABLE dbo.Items NOCHECK CONSTRAINT FK_Items_Condition;

-- Update existing condition names to match frontend
UPDATE dbo.ConditionTypes SET Name = 'Like New' WHERE ConditionID = 2;
UPDATE dbo.ConditionTypes SET Name = 'Good' WHERE ConditionID = 3;
UPDATE dbo.ConditionTypes SET Name = 'Fair' WHERE ConditionID = 4;

-- Add the 5th condition if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM dbo.ConditionTypes WHERE ConditionID = 5)
BEGIN
    INSERT INTO dbo.ConditionTypes (ConditionID, Name) VALUES (5, 'Poor');
    PRINT 'Added condition: Poor (ID: 5)';
END
ELSE
BEGIN
    -- If it exists, make sure the name is correct
    UPDATE dbo.ConditionTypes SET Name = 'Poor' WHERE ConditionID = 5;
    PRINT 'Updated condition: Poor (ID: 5)';
END

-- Re-enable constraints
ALTER TABLE dbo.Items CHECK CONSTRAINT FK_Items_Condition;

COMMIT TRANSACTION;

-- Display the final state
SELECT ConditionID, Name FROM dbo.ConditionTypes ORDER BY ConditionID;
GO
