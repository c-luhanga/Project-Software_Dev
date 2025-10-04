// This file now serves as re-exports to maintain backward compatibility
// All actual entity definitions are now in UniShareProject.DataBases.Entities

namespace UniShareProject.Repository.Models;

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
