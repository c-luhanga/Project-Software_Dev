using System;
using System.Collections.Generic;

namespace UniShare.Data
{
    public class User
    {
        public int Id { get; set; }
        public string? FirebaseUid { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? PasswordHash { get; set; }
        public string? House { get; set; }
        public bool IsBanned { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? ProfileImageUrl { get; set; }
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<ConversationParticipant> Conversations { get; set; } = new List<ConversationParticipant>();
    }

    public class Item
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Condition { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public DateTime PostedDate { get; set; }
        public User Seller { get; set; } = null!;
        public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    }

    public class ItemImage
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public Item Item { get; set; } = null!;
    }

    public class Conversation
    {
        public int Id { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastUpdated { get; set; }
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class ConversationParticipant
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
