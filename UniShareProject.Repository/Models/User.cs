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
