-- Add "Poor" condition type if it doesn't exist
-- This script is idempotent - safe to run multiple times

IF NOT EXISTS (SELECT 1 FROM dbo.ConditionTypes WHERE ConditionID = 5)
BEGIN
    INSERT INTO dbo.ConditionTypes (ConditionID, Name)
    VALUES (5, 'Poor')
    PRINT 'Added condition type: Poor (ID: 5)'
END
ELSE
BEGIN
    PRINT 'Condition type "Poor" already exists'
END
GO
