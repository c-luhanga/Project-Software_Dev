// This file now serves as re-exports to maintain backward compatibility
// All actual entity definitions are now in UniShareProject.DataBases.Entities

using System.Text.Json.Serialization;

namespace UniShareProject.Repository.Models;

public class Item
{
    // Primary key - matches database ItemID field
    public int ItemID { get; set; }
    
    // Backward compatibility - ignore during JSON serialization
    [JsonIgnore]
    public int Id 
    { 
        get => ItemID; 
        set => ItemID = value; 
    }
    
    [JsonIgnore]
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
    
    // Backward compatibility properties - ignore during JSON serialization
    [JsonIgnore]
    public int? CategoryId 
    { 
        get => CategoryID; 
        set => CategoryID = value; 
    }
    
    [JsonIgnore]
    public byte ConditionId 
    { 
        get => ConditionID; 
        set => ConditionID = value; 
    }
    
    [JsonIgnore]
    public byte StatusId 
    { 
        get => StatusID; 
        set => StatusID = value; 
    }
    
    [JsonIgnore]
    public int SellerId 
    { 
        get => SellerID; 
        set => SellerID = value; 
    }
    
    [JsonIgnore]
    public string Category { get; set; } = string.Empty; // For backward compatibility
    
    [JsonIgnore]
    public bool IsAvailable 
    { 
        get => StatusID == 1; // Status 1 = available
        set => StatusID = (byte)(value ? 1 : 0); 
    }
    
    [JsonIgnore]
    public int OwnerId 
    { 
        get => SellerID; 
        set => SellerID = value; 
    }
    
    [JsonIgnore]
    public DateTime CreatedAt 
    { 
        get => PostedDate; 
        set => PostedDate = value; 
    }
    
    [JsonIgnore]
    public DateTime UpdatedAt 
    { 
        get => PostedDate;
        set { } 
    }
}
