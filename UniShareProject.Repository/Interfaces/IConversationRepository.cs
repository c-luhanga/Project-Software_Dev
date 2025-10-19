using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;

namespace UniShareProject.Repository.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id, IUnitOfWork unitOfWork);
    Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    Task<int> CreateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> UpdateAsync(Conversation conversation, IUnitOfWork unitOfWork);
    Task<bool> DeleteAsync(int id, IUnitOfWork unitOfWork);
    
    /// <summary>
    /// Get conversation list items for a user with other participant info and unread counts
    /// </summary>
    Task<IEnumerable<ConversationListData>> GetConversationListByUserIdAsync(int userId, IUnitOfWork unitOfWork);
    
    /// <summary>
    /// Check if user is a participant in the conversation
    /// </summary>
    Task<bool> IsUserParticipantAsync(int conversationId, int userId, IUnitOfWork unitOfWork);
    
    /// <summary>
    /// Find existing conversation between users for a specific item
    /// </summary>
    Task<int?> FindExistingConversationAsync(int itemId, int userId1, int userId2, IUnitOfWork unitOfWork);
}

/// <summary>
/// Data structure for conversation list queries
/// </summary>
public class ConversationListData
{
    public int ConversationID { get; set; }
    public int? ItemID { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastUpdated { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}
