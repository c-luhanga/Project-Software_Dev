# Profile Update Fix - Partial Update Support ?

## Problem

When updating a single profile field (e.g., just `house`), all other fields (phone, profileImageUrl) were being reset to `null`. This happened because the repository was blindly updating all three fields with whatever was passed in, including `null` values for fields that weren't meant to be changed.

### Example of the Bug

**Request:**
```json
PUT /api/users/me
{
  "house": "Anderson Hall"
}
```

**Before Fix:**
- ? House ? "Anderson Hall"  
- ? Phone ? `null` (was reset!)
- ? ProfileImageUrl ? `null` (was reset!)

**After Fix:**
- ? House ? "Anderson Hall"
- ? Phone ? (preserved existing value)
- ? ProfileImageUrl ? (preserved existing value)

---

## Solution

Modified `UserService.UpdateMeAsync()` to:

1. **Fetch current user data** before updating
2. **Preserve existing values** for fields not provided in the request
3. **Support three update modes**:
   - `null` in JSON = **don't change** (keep existing value)
   - Empty string `""` in JSON = **clear field** (set to null in DB)
   - Non-empty value = **update** to new value

### Implementation

```csharp
public async Task<UserDto> UpdateMeAsync(int userId, UpdateMeRequest req, CancellationToken ct)
{
    // 1. Fetch current user
    var currentUser = await _userRepository.GetByIdAsync(userId, ct);
  
    // 2. Determine what to update for each field
    string? phoneToUpdate;
    if (req.Phone == null)
 phoneToUpdate = currentUser.Phone;  // Keep existing
    else if (string.IsNullOrWhiteSpace(req.Phone))
        phoneToUpdate = null;  // Clear field
    else
        phoneToUpdate = req.Phone;  // Update to new value
    
    // ... similar logic for house and profileImageUrl
    
    // 3. Update with resolved values
    await _userRepository.UpdateProfileAsync(userId, phoneToUpdate, houseToUpdate, profileImageUrlToUpdate, ct);
}
```

---

## Updated Behavior

### Scenario 1: Update only house
**Request:**
```json
{
  "house": "Anderson Hall"
}
```
**Result:**
- House: "Anderson Hall" ?
- Phone: (preserved) ?
- ProfileImageUrl: (preserved) ?

---

### Scenario 2: Update only phone
**Request:**
```json
{
  "phone": "+1-555-999-8888"
}
```
**Result:**
- Phone: "+1-555-999-8888" ?
- House: (preserved) ?
- ProfileImageUrl: (preserved) ?

---

### Scenario 3: Update multiple fields
**Request:**
```json
{
  "phone": "+1-555-123-4567",
  "house": "Johnson Hall"
}
```
**Result:**
- Phone: "+1-555-123-4567" ?
- House: "Johnson Hall" ?
- ProfileImageUrl: (preserved) ?

---

### Scenario 4: Clear a field (set to null)
**Request:**
```json
{
  "phone": "",
  "house": ""
}
```
**Result:**
- Phone: `null` ?
- House: `null` ?
- ProfileImageUrl: (preserved) ?

---

### Scenario 5: Mixed operations
**Request:**
```json
{
  "phone": "+1-555-NEW-NUM",
  "house": "",
  "profileImageUrl": null
}
```
**Result:**
- Phone: "+1-555-NEW-NUM" (updated) ?
- House: `null` (cleared) ?
- ProfileImageUrl: (preserved) ?

---

## Files Modified

### 1. `UniShareProject.services/Implementations/UserService.cs`
- Modified `UpdateMeAsync()` method
- Added logic to fetch current user before updating
- Implemented three-state field resolution (preserve/clear/update)

### 2. `UniShareProject.Repository/Implementations/UserRepository.cs`
- Simplified `UpdateProfileAsync()` - no longer needs dynamic SQL
- Service layer now handles all field resolution

---

## Testing

### Test Cases

| Test | Request Body | Expected Behavior |
|------|-------------|-------------------|
| USER-04 | `{"phone": "+1-555-999-8888"}` | Updates phone only, preserves house & image |
| USER-05 | `{"house": "Anderson Hall"}` | Updates house only, preserves phone & image |
| USER-06 | `{"profileImageUrl": "https://..."}` | Updates image only, preserves phone & house |
| USER-07 | `{"phone": "", "house": "", "profileImageUrl": ""}` | Clears all three fields (sets to null) |
| USER-02 | `{"phone": "...", "house": "...", "profileImageUrl": "..."}` | Updates all three fields |

### HTTP Test File Updates

The existing tests in `UniShareProject.API.http` now work correctly:

```http
### USER-05: Update Profile - Partial (House Only)
PUT {{UniShareProject.API_HostAddress}}/api/users/me
Content-Type: application/json
Authorization: Bearer {{authToken}}

{
  "house": "Anderson Hall"
}
```

? This now correctly preserves `phone` and `profileImageUrl`

---

## API Contract

### Request Model: `UpdateMeRequest`

```csharp
public record UpdateMeRequest(
    string? Phone,      // null = don't change, "" = clear, "value" = update
    string? House,           // null = don't change, "" = clear, "value" = update
    string? ProfileImageUrl  // null = don't change, "" = clear, "value" = update
);
```

### Field Update Rules

| JSON Value | Meaning | Action |
|-----------|---------|--------|
| Field omitted | Don't change | Preserve existing value |
| `null` | Don't change | Preserve existing value |
| `""` (empty string) | Clear field | Set to `null` in database |
| `"value"` | Update | Set to new value |

---

## Benefits

1. ? **Truly partial updates** - only change what you intend to change
2. ? **Backward compatible** - existing API clients continue to work
3. ? **Explicit clear operation** - can intentionally remove data using empty strings
4. ? **Intuitive behavior** - matches REST PATCH semantics
5. ? **No breaking changes** - same endpoint, same request format

---

## Notes

- This fix aligns with REST PATCH semantics where only provided fields are updated
- The approach uses empty strings to distinguish "clear" from "don't touch"
- All existing validation rules still apply (max lengths, URL formats, etc.)
- The `LastSeen` timestamp is always updated regardless of which fields change

---

## Related Issues

- ? Fixes USER-05 TODO in `UniShareProject.API.http`
- ? Enables true partial profile updates
- ? Prevents accidental data loss when updating single fields

---

**Status**: ? Fix implemented and tested
**Build**: ? Successful
**Breaking Changes**: ? None
**Date**: 2024
