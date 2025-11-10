using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ConnectionProject.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time messaging functionality
    /// Handles WebSocket connections for instant message delivery
    /// </summary>
    [Authorize]
    public class MessagingHub : Hub
    {
        private readonly ILogger<MessagingHub> _logger;

        public MessagingHub(ILogger<MessagingHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                // Add user to their personal group for receiving messages
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("[SignalR] User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("[SignalR] User connected without valid userId, connection {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                // Remove user from their personal group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("[SignalR] User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
            }

            if (exception != null)
            {
                _logger.LogError(exception, "[SignalR] User disconnected with error: {Error}", exception.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a conversation group for real-time message updates
        /// </summary>
        /// <param name="conversationId">The conversation ID to join</param>
        public async Task JoinConversation(string conversationId)
        {
            var userId = GetUserId();
            if (userId != null && int.TryParse(conversationId, out var convId))
            {
                var groupName = $"Conversation_{convId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation("[SignalR] User {UserId} joined conversation {ConversationId}", userId, convId);
            }
        }

        /// <summary>
        /// Leave a conversation group
        /// </summary>
        /// <param name="conversationId">The conversation ID to leave</param>
        public async Task LeaveConversation(string conversationId)
        {
            var userId = GetUserId();
            if (userId != null && int.TryParse(conversationId, out var convId))
            {
                var groupName = $"Conversation_{convId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation("[SignalR] User {UserId} left conversation {ConversationId}", userId, convId);
            }
        }

        /// <summary>
        /// Heartbeat method to keep connection alive and verify connectivity
        /// </summary>
        public async Task Heartbeat()
        {
            var userId = GetUserId();
            await Clients.Caller.SendAsync("HeartbeatResponse", new { 
                timestamp = DateTime.UtcNow,
                userId = userId,
                status = "connected"
            });
        }

        /// <summary>
        /// Get the current user's ID from the JWT claims
        /// </summary>
        private string? GetUserId()
        {
            return Context.User?.FindFirst("sub")?.Value 
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Get the current user's email from the JWT claims
        /// </summary>
        private string? GetUserEmail()
        {
            return Context.User?.FindFirst("email")?.Value
                ?? Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    /// <summary>
    /// Extension methods for messaging-specific SignalR operations
    /// </summary>
    public static class MessagingHubExtensions
    {
        /// <summary>
        /// Send a new message notification to all participants in a conversation
        /// </summary>
        public static async Task NotifyNewMessage(this IHubContext<MessagingHub> hubContext, 
            int conversationId, 
            object messageData)
        {
            await hubContext.Clients.Group($"Conversation_{conversationId}")
                .SendAsync("NewMessage", messageData);
        }

        /// <summary>
        /// Send a conversation update notification to a specific user
        /// </summary>
        public static async Task NotifyConversationUpdate(this IHubContext<MessagingHub> hubContext, 
            string userId, 
            object conversationData)
        {
            await hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ConversationUpdate", conversationData);
        }

        /// <summary>
        /// Notify users when someone starts typing in a conversation
        /// </summary>
        public static async Task NotifyTyping(this IHubContext<MessagingHub> hubContext, 
            int conversationId, 
            string userId, 
            string userName)
        {
            await hubContext.Clients.GroupExcept($"Conversation_{conversationId}", userId)
                .SendAsync("UserTyping", new { userId, userName, conversationId });
        }

        /// <summary>
        /// Notify users when someone stops typing in a conversation
        /// </summary>
        public static async Task NotifyStoppedTyping(this IHubContext<MessagingHub> hubContext, 
            int conversationId, 
            string userId)
        {
            await hubContext.Clients.GroupExcept($"Conversation_{conversationId}", userId)
                .SendAsync("UserStoppedTyping", new { userId, conversationId });
        }
    }
}