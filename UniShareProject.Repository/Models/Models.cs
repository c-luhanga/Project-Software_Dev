// This file now serves as re-exports to maintain backward compatibility
// All actual entity definitions are now in UniShareProject.DataBases.Entities

namespace UniShareProject.Repository.Models;

public class User
{
    // Primary key - matches database UserID field
    public int UserID { get; set; }
    
    // Backward compatibility property for old code that uses UserId
    public int UserId 
    { 
        get => UserID; 
        set => UserID = value; 
    }
    
    // Additional backward compatibility
    public int Id 
    { 
        get => UserID; 
        set => UserID = value; 
    }
    
    // Database fields matching the actual schema
    public string? FirebaseUID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? House { get; set; }
    public bool IsBanned { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
    public string? ProfileImageURL { get; set; }
    
    // Backward compatibility properties
    public string Username 
    { 
        get => Email;
        set => Email = value; 
    }
    
    public DateTime UpdatedAt 
    { 
        get => CreatedAt;
        set { } 
    }
    
    public bool IsActive 
    { 
        get => !IsDeleted; 
        set => IsDeleted = !value; 
    }
}

public class Item
{
    // Primary key - matches database ItemID field
    public int ItemID { get; set; }
    
    // Backward compatibility
    public int Id 
    { 
        get => ItemID; 
        set => ItemID = value; 
    }
    
    public int ItemId 
    { 
        get => ItemID; 
        set => ItemID = value; 
    }
    
    // Database fields matching the actual schema
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CategoryID { get; set; }
    public decimal? Price { get; set; }
    public byte ConditionID { get; set; }
    public byte StatusID { get; set; }
    public int SellerID { get; set; }
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    
    // Backward compatibility properties
    public int? CategoryId 
    { 
        get => CategoryID; 
        set => CategoryID = value; 
    }
    
    public byte ConditionId 
    { 
        get => ConditionID; 
        set => ConditionID = value; 
    }
    
    public byte StatusId 
    { 
        get => StatusID; 
        set => StatusID = value; 
    }
    
    public int SellerId 
    { 
        get => SellerID; 
        set => SellerID = value; 
    }
    
    public string Category { get; set; } = string.Empty; // For backward compatibility
    
    public bool IsAvailable 
    { 
        get => StatusID == 1; // Status 1 = available
        set => StatusID = (byte)(value ? 1 : 0); 
    }
    
    public int OwnerId 
    { 
        get => SellerID; 
        set => SellerID = value; 
    }
    
    public DateTime CreatedAt 
    { 
        get => PostedDate; 
        set => PostedDate = value; 
    }
    
    public DateTime UpdatedAt 
    { 
        get => PostedDate;
        set { } 
    }
}

public class Conversation
{
    public int ConversationID { get; set; }
    
    // Backward compatibility
    public int Id 
    { 
        get => ConversationID; 
        set => ConversationID = value; 
    }
    
    public int? ItemID { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Backward compatibility properties
    public int ItemId 
    { 
        get => ItemID ?? 0; 
        set => ItemID = value; 
    }
    
    public DateTime UpdatedAt 
    { 
        get => LastUpdated; 
        set => LastUpdated = value; 
    }
    
    public bool IsActive { get; set; } = true;
    
    // These properties might be derived from ConversationParticipants table
    public int BuyerId { get; set; }
    public int SellerId { get; set; }
}

public class Message
{
    public int MessageID { get; set; }
    
    // Backward compatibility
    public int Id 
    { 
        get => MessageID; 
        set => MessageID = value; 
    }
    
    public int ConversationID { get; set; }
    public int SenderID { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Backward compatibility properties
    public int ConversationId 
    { 
        get => ConversationID; 
        set => ConversationID = value; 
    }
    
    public int SenderId 
    { 
        get => SenderID; 
        set => SenderID = value; 
    }
    
    public DateTime CreatedAt 
    { 
        get => Timestamp; 
        set => Timestamp = value; 
    }
    
    public bool IsRead { get; set; } = false;
}