namespace UniShareProject.services.Interfaces;

/// <summary>
/// Interface for real-time notification services
/// Abstracts the underlying real-time communication technology (SignalR, WebSockets, etc.)
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Notify users about a new message in a conversation
    /// </summary>
    /// <param name="conversationId">The conversation where the message was sent</param>
    /// <param name="message">The message data to broadcast</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyNewMessageAsync(int conversationId, object message, CancellationToken ct = default);

    /// <summary>
    /// Notify a specific user about conversation updates (unread counts, etc.)
    /// </summary>
    /// <param name="userId">The user to notify</param>
    /// <param name="conversationData">The conversation update data</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyConversationUpdateAsync(string userId, object conversationData, CancellationToken ct = default);

    /// <summary>
    /// Notify users when someone starts typing in a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="userId">The user who is typing</param>
    /// <param name="userName">The name of the user who is typing</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyTypingAsync(int conversationId, string userId, string userName, CancellationToken ct = default);

    /// <summary>
    /// Notify users when someone stops typing in a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="userId">The user who stopped typing</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyStoppedTypingAsync(int conversationId, string userId, CancellationToken ct = default);
}