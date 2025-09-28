using UniShareProject.Repository.Models;

namespace UniShareProject.services.Services;

public interface IItemService
{
    Task<Item?> GetByIdAsync(int id);
    Task<IEnumerable<Item>> GetAllAsync();
    Task<IEnumerable<Item>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Item>> SearchAsync(string query);
    Task<Item> CreateAsync(Item item);
    Task<Item> UpdateAsync(Item item);
    Task<bool> DeleteAsync(int id);
}

public interface IMessagingService
{
    Task<Conversation?> GetConversationByIdAsync(int id);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId);
    Task<Conversation> CreateConversationAsync(int itemId, int buyerId, int sellerId);
    Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId);
    Task<Message> SendMessageAsync(int conversationId, int senderId, string content);
    Task<bool> MarkMessageAsReadAsync(int messageId);
}