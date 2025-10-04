// This file now serves as re-exports to maintain backward compatibility
// All actual entity definitions are now in UniShareProject.DataBases.Entities

namespace UniShareProject.Repository.Models;

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
