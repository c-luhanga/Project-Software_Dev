using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Controller for messaging operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessagingService messagingService, ILogger<MessagesController> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of conversation items</returns>
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<ConversationListItem>>> GetConversations(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var conversations = await _messagingService.GetUserConversationsAsync(userId, ct);
        return Ok(conversations);
    }

    /// <summary>
    /// Start a new conversation with another user
    /// </summary>
    /// <param name="request">Start conversation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The conversation ID</returns>
    [HttpPost("conversations")]
    public async Task<ActionResult<int>> StartConversation([FromBody] StartConversationRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var conversationId = await _messagingService.StartConversationAsync(userId, request, ct);
        return Ok(new { ConversationId = conversationId });
    }

    /// <summary>
    /// Get all messages in a conversation
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of messages</returns>
    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversationMessages(int conversationId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        
        try
        {
            var messages = await _messagingService.GetConversationMessagesAsync(conversationId, userId, ct);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You are not a participant in this conversation.");
        }
    }

    /// <summary>
    /// Send a message in an existing conversation
    /// </summary>
    /// <param name="request">Send message request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The sent message</returns>
    [HttpPost("messages")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        
        try
        {
            var message = await _messagingService.SendMessageAsync(userId, request, ct);
            return Ok(message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You are not a participant in this conversation.");
        }
    }

    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("conversations/{conversationId:int}/read")]
    public async Task<ActionResult> MarkConversationAsRead(int conversationId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var success = await _messagingService.MarkConversationAsReadAsync(conversationId, userId, ct);
        
        if (!success)
        {
            return NotFound("Conversation not found or you are not a participant.");
        }
        
        return Ok(new { Message = "Conversation marked as read" });
    }

    /// <summary>
    /// Helper method to get the current user ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        
        return userId;
    }
}