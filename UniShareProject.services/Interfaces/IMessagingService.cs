using UniShareProject.services.DTOs;
using UniShareProject.Repository.Models;

namespace UniShareProject.services.Interfaces;

/// <summary>
/// Service interface for messaging operations
/// </summary>
public interface IMessagingService
{
    /// <summary>
    /// Start a new conversation with another user
    /// </summary>
    /// <param name="req">Start conversation request</param>
    /// <param name="starterUserId">ID of the user starting the conversation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The conversation ID</returns>
    Task<int> StartConversationAsync(StartConversationRequest req, int starterUserId, CancellationToken ct);

    /// <summary>
    /// Send a message in an existing conversation
    /// </summary>
    /// <param name="req">Send message request</param>
    /// <param name="senderId">ID of the user sending the message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The sent message</returns>
    Task<MessageDto> SendAsync(SendMessageRequest req, int senderId, CancellationToken ct);

    /// <summary>
    /// Get paged messages for a conversation
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="page">Pagination specification</param>
    /// <param name="userId">ID of the current user (for authorization)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of messages</returns>
    Task<PagedResult<MessageDto>> GetConversationAsync(int conversationId, PageSpec page, int userId, CancellationToken ct);

    /// <summary>
    /// Get paged conversations for a user's inbox
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="page">Pagination specification</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of conversation items</returns>
    Task<PagedResult<ConversationListItem>> ListForUserAsync(int userId, PageSpec page, CancellationToken ct);
}
