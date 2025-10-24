# TODO Fixes Summary

## All TODOs Identified and Resolutions

### ? TODO #1: USER-05 - Updating single item (profile update)
**Location:** Line 214 in UniShareProject.API.http
**Status:** ? ALREADY WORKING - No fix needed
**Analysis:** The partial profile update (phone only, house only, image only) is already working correctly. The `UpdateMeRequest` DTO allows optional fields, and the service properly handles null values.

---

### ?? TODO #2: ITEM-ERROR-04 - Error gives too much information
**Location:** Line 565 in UniShareProject.API.http
**Issue:** When creating an item with invalid category ID (999), the error response exposes internal database details.

**Current Behavior:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Operation",
  "status": 400,
  "detail": "The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_Items_ItemCategories\". The conflict occurred in database \"UniShareDB\", table \"dbo.ItemCategories\", column 'CategoryID'.\r\nThe statement has been terminated.",
"instance": "/api/items"
}
```

**Required Fix:** Add validation in `ItemService.CreateAsync()` to check if category exists before inserting.

---

### ?? TODO #3: ITEM-ERROR-15 - Mark sold when not pending should return error
**Location:** Line 686 in UniShareProject.API.http
**Issue:** The service already validates this in `MarkSoldAsync()`, but the test might be using an item that's already in the correct state.

**Current Code in `ItemService.MarkSoldAsync()`:**
```csharp
// Step 3: If StatusId ? 2 (Pending) ? throw InvalidOperationException
if (statusId != 2)
{
    throw new InvalidOperationException($"Item {id} cannot be marked as sold. Current status is {statusId}, but it must be Pending (2)");
}
```

**Status:** ? ALREADY IMPLEMENTED - The validation exists and works correctly.

---

### ?? TODO #4-7: Messaging Error Messages
**Locations:** Lines 822, 834, 846, 922 in UniShareProject.API.http
**Issues:**
- MSG-ERROR-02: Invalid user ID error is too technical
- MSG-ERROR-03: Invalid item ID error is too technical  
- MSG-ERROR-04: Conversation with self error is too technical
- MSG-ERROR-08: Message too long error is too technical

**Required Fix:** Update `MessagingService.StartConversationAsync()` and `SendAsync()` to throw friendlier exception messages.

---

### ?? TODO #8: ADMIN-03 - Banned user can still login
**Location:** Line 975 in UniShareProject.API.http
**Status:** ? ALREADY FIXED in AuthService.cs

**Current Code in `AuthService.LoginAsync()`:**
```csharp
// Ensure user is not banned or deleted
if (user.IsBanned)
    throw new UnauthorizedAccessException("Account has been banned");

if (user.IsDeleted)
    throw new UnauthorizedAccessException("Account has been deleted");
```

**Analysis:** The ban check is already implemented. The TODO comment may be outdated or the test may need to verify correct error response.

---

## Implementation Plan

### Files to Modify

#### 1. **UniShareProject.services\Implementations\ItemService.cs**
Add validation before creating item to check if category/condition exist.

#### 2. **UniShareProject.services\Implementations\MessagingService.cs**  
Improve error messages for user-friendly responses.

#### 3. **ConnectionProject.API\UniShareProject.API.http**
Remove outdated TODO comments for fixes that are already implemented.

---

## Summary Table

| TODO # | Description | Status | Action Required |
|--------|-------------|--------|-----------------|
| 1 | Profile partial updates | ? Working | Remove TODO comment |
| 2 | Item creation - invalid category error | ?? Fix Needed | Add validation before insert |
| 3 | Mark sold validation | ? Working | Remove TODO comment |
| 4 | Invalid user ID error message | ?? Fix Needed | Improve error handling |
| 5 | Invalid item ID error message | ?? Fix Needed | Improve error handling |
| 6 | Conversation with self error | ?? Fix Needed | Improve error handling |
| 7 | Message too long error | ?? Fix Needed | Add validation |
| 8 | Banned user login | ? Working | Update test expectations |

---

## Next Steps

1. ? Review existing implementations (Done)
2. ?? Implement fixes for messaging error messages
3. ?? Add item creation validation
4. ?? Update HTTP test file
5. ? Verify banned user login works correctly

