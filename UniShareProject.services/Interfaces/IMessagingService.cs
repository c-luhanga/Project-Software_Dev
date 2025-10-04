using UniShareProject.Repository.Models;

namespace UniShareProject.services.Interfaces;

public interface IMessagingService
{
    Task<Conversation?> GetConversationByIdAsync(int id);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId);
    Task<Conversation> CreateConversationAsync(int itemId, int buyerId, int sellerId);
    Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId);
    Task<Message> SendMessageAsync(int conversationId, int senderId, string content);
    Task<bool> MarkMessageAsReadAsync(int messageId);
}
