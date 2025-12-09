# Fixes Implementation Plan

## Summary of Issues from UniShareProject.API.http

### ? Already Fixed/Working
1. **USER-05 (Line 214)**: Updating single profile field - Already working correctly
2. **ITEM-ERROR-15 (Line 686)**: Mark sold validation - Already implemented in `ItemService.MarkSoldAsync()`
3. **ADMIN-03 (Line 975)**: Banned user login - Already prevented in `AuthService.LoginAsync()`

### ?? Requires Fixes

#### 1. **ITEM-ERROR-04 (Line 565)**: Database error exposes too much information
**Problem**: Creating item with invalid category ID (999) returns SQL constraint error with database details.

**Solution**: Add validation in `ItemService.CreateAsync()` before insertion to check category/condition existence.

#### 2. **MSG-ERROR-02 (Line 822)**: Invalid user ID error
**Problem**: Technical database error when starting conversation with non-existent user.

**Solution**: Add user existence validation in `MessagingService.StartConversationAsync()` with friendly error.

#### 3. **MSG-ERROR-03 (Line 834)**: Invalid item ID error  
**Problem**: Technical database error when starting conversation about non-existent item.

**Solution**: Add item existence validation in `MessagingService.StartConversationAsync()` with friendly error.

#### 4. **MSG-ERROR-04 (Line 846)**: Conversation with self error
**Problem**: Generic error when user tries to message themselves.

**Solution**: Add validation in `MessagingService.StartConversationAsync()` to prevent self-messaging.

#### 5. **MSG-ERROR-08 (Line 922)**: Message too long error
**Problem**: No validation for message length (max 4000 chars).

**Solution**: Add validation in `MessagingService.SendAsync()` to check message length.

## Implementation Details

### File: `UniShareProject.services/Implementations/ItemService.cs`

**Method**: `CreateAsync(CreateItemRequest req, int sellerId, CancellationToken ct)`

Add validation before insert:
```csharp
// Validate category exists (1-6 are valid)
if (req.CategoryId < 1 || req.CategoryId > 6)
{
    throw new ArgumentException($"Invalid category ID {req.CategoryId}. Valid categories are 1-6.");
}

// Validate condition exists (1-4 are valid)
if (req.ConditionId < 1 || req.ConditionId > 4)
{
    throw new ArgumentException($"Invalid condition ID {req.ConditionId}. Valid conditions are 1-4.");
}
```

### File: `UniShareProject.services/Implementations/MessagingService.cs`

**Method**: `StartConversationAsync(StartConversationRequest req, int starterUserId, CancellationToken ct)`

Add validations:
```csharp
// 1. Check if trying to message self
if (req.OtherUserId == starterUserId)
{
    throw new ArgumentException("You cannot start a conversation with yourself.");
}

// 2. Check if other user exists (using UserRepository)
var otherUserExists = await _userRepository.UserExistsAsync(req.OtherUserId, ct);
if (!otherUserExists)
{
    throw new ArgumentException($"User with ID {req.OtherUserId} not found.");
}

// 3. Check if item exists (if itemId provided)
if (req.ItemId.HasValue)
{
    var item = await _itemRepository.GetByIdAsync(req.ItemId.Value, ct);
    if (item == null)
    {
        throw new ArgumentException($"Item with ID {req.ItemId.Value} not found.");
    }
}
```

**Method**: `SendAsync(SendMessageRequest req, int senderId, CancellationToken ct)`

Add validation:
```csharp
// Validate message length
if (string.IsNullOrWhiteSpace(req.Content))
{
    throw new ArgumentException("Message content cannot be empty.");
}

if (req.Content.Length > 4000)
{
    throw new ArgumentException($"Message content exceeds maximum length of 4000 characters. Current length: {req.Content.Length}");
}
```

### File: `ConnectionProject.API/UniShareProject.API.http`

Remove TODO comments for already working features:
- Line 214: USER-05
- Line 686: ITEM-ERROR-15  
- Line 975: ADMIN-03

## Testing Plan

After implementing fixes, test the following scenarios:

1. **ITEM-ERROR-04**: Create item with categoryId=999, expect user-friendly error
2. **MSG-ERROR-02**: Start conversation with userId=999999, expect "User not found"
3. **MSG-ERROR-03**: Start conversation with itemId=999999, expect "Item not found"
4. **MSG-ERROR-04**: Start conversation with self (otherUserId = own userId), expect "cannot message yourself"
5. **MSG-ERROR-08**: Send message > 4000 chars, expect length validation error

## Dependencies Required

`MessagingService` needs access to:
- `IUserRepository` (for user existence check)
- `IItemRepository` (for item existence check)

Add constructor injection if not already present.
