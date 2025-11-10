using Microsoft.AspNetCore.SignalR;
using UniShareProject.services.Interfaces;
using ConnectionProject.API.Hubs;

namespace ConnectionProject.API.Services;

/// <summary>
/// SignalR implementation of real-time notification service
/// </summary>
public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<MessagingHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<MessagingHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewMessageAsync(int conversationId, object message, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group($"Conversation_{conversationId}")
                .SendAsync("NewMessage", message, ct);
            
            _logger.LogDebug("Sent new message notification to conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new message notification to conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task NotifyConversationUpdateAsync(string userId, object conversationData, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ConversationUpdate", conversationData, ct);
            
            _logger.LogDebug("Sent conversation update notification to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send conversation update notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task NotifyTypingAsync(int conversationId, string userId, string userName, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.GroupExcept($"Conversation_{conversationId}", userId)
                .SendAsync("UserTyping", new { userId, userName, conversationId }, ct);
            
            _logger.LogDebug("Sent typing notification for user {UserId} in conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing notification for user {UserId} in conversation {ConversationId}", userId, conversationId);
            throw;
        }
    }

    public async Task NotifyStoppedTypingAsync(int conversationId, string userId, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.GroupExcept($"Conversation_{conversationId}", userId)
                .SendAsync("UserStoppedTyping", new { userId, conversationId }, ct);
            
            _logger.LogDebug("Sent stopped typing notification for user {UserId} in conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send stopped typing notification for user {UserId} in conversation {ConversationId}", userId, conversationId);
            throw;
        }
    }
}