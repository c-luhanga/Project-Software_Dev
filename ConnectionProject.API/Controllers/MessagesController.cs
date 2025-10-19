using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;
using UniShareProject.Repository.Models;

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
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of conversation items</returns>
    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ConversationListItem>>> GetConversations(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var pageSpec = new PageSpec(page, pageSize);
        var conversations = await _messagingService.ListForUserAsync(userId, pageSpec, ct);
        return Ok(conversations);
    }

    /// <summary>
    /// Start a new conversation with another user
    /// </summary>
    /// <param name="request">Start conversation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The conversation ID</returns>
    [HttpPost("conversations")]
    public async Task<ActionResult<int>> StartConversation([FromBody] StartConversationRequest request, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var conversationId = await _messagingService.StartConversationAsync(request, userId, ct);
        return Ok(new { ConversationId = conversationId });
    }

    /// <summary>
    /// Get all messages in a conversation
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 50)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of messages</returns>
    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<PagedResult<MessageDto>>> GetConversationMessages(
        int conversationId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50, 
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var pageSpec = new PageSpec(page, pageSize);
        
        try
        {
            var messages = await _messagingService.GetConversationAsync(conversationId, pageSpec, userId, ct);
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
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        
        try
        {
            var message = await _messagingService.SendAsync(request, userId, ct);
            return Ok(message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You are not a participant in this conversation.");
        }
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