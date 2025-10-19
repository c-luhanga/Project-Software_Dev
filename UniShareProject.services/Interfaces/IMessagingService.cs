using UniShareProject.services.DTOs;

namespace UniShareProject.services.Interfaces;

/// <summary>
/// Service interface for messaging operations
/// </summary>
public interface IMessagingService
{
    /// <summary>
    /// Start a new conversation with another user
    /// </summary>
    /// <param name="currentUserId">ID of the current user</param>
    /// <param name="request">Start conversation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The conversation ID</returns>
    Task<int> StartConversationAsync(int currentUserId, StartConversationRequest request, CancellationToken ct);

    /// <summary>
    /// Send a message in an existing conversation
    /// </summary>
    /// <param name="currentUserId">ID of the current user</param>
    /// <param name="request">Send message request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The sent message</returns>
    Task<MessageDto> SendMessageAsync(int currentUserId, SendMessageRequest request, CancellationToken ct);

    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of conversation items</returns>
    Task<IEnumerable<ConversationListItem>> GetUserConversationsAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Get all messages in a conversation
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="userId">ID of the current user (for authorization)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of messages</returns>
    Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, CancellationToken ct);

    /// <summary>
    /// Mark messages in a conversation as read
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="userId">ID of the user marking messages as read</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> MarkConversationAsReadAsync(int conversationId, int userId, CancellationToken ct);
}
