// This file now serves as re-exports to maintain backward compatibility
// All actual entity definitions are now in UniShareProject.DataBases.Entities

namespace UniShareProject.Repository.Models;

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
