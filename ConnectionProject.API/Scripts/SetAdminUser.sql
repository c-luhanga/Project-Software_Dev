-- SQL Script to set a user as admin
-- Replace 'admin.user@principia.edu' with your actual admin email if different

-- Update the user to be an admin
UPDATE dbo.Users
SET IsAdmin = 1
WHERE Email = 'admin.user@principia.edu';

-- Verify the update
SELECT UserID, FirstName, LastName, Email, IsAdmin, IsBanned, CreatedAt
FROM dbo.Users
WHERE Email = 'admin.user@principia.edu';
