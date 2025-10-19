using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;
using UniShareProject.Repository.Models;
using ConnectionProject.API.Controllers.Base;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Controller for messaging operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class MessagesController : BaseApiController
{
    private readonly IMessagingService _messagingService;

    public MessagesController(IMessagingService messagingService, ILogger<MessagesController> logger) 
        : base(logger)
    {
        _messagingService = messagingService;
    }

    /// <summary>
    /// Start a new conversation with another user
    /// </summary>
    /// <param name="request">Start conversation request containing other user ID and optional item ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created conversation ID</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/messages/conversations
    ///     {
    ///       "otherUserId": 123,
    ///       "itemId": 456
    ///     }
    /// 
    /// This endpoint will either create a new conversation or return an existing one
    /// between the current user and the specified other user for the given item.
    /// If no item is specified, a general conversation will be created.
    /// </remarks>
    /// <response code="200">Conversation started successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission</response>
    /// <response code="404">Other user or item not found</response>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request, CancellationToken ct)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            Logger.LogInformation("[Messages] User {UserId} starting conversation with user {OtherUserId} for item {ItemId}", 
                currentUserId, request.OtherUserId, request.ItemId);

            var conversationId = await _messagingService.StartConversationAsync(request, currentUserId, ct);
            
            Logger.LogInformation("[Messages] Conversation {ConversationId} started/found successfully", conversationId);
            return Ok(new { ConversationId = conversationId });
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "[Messages] Invalid request for starting conversation");
            return BadRequest(new { Error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "[Messages] Unauthorized attempt to start conversation");
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Send a message in an existing conversation
    /// </summary>
    /// <param name="request">Send message request containing conversation ID and message content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The sent message details</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/messages/send
    ///     {
    ///       "conversationId": 123,
    ///       "content": "Hello! Is this item still available?"
    ///     }
    /// 
    /// The message content must be between 1 and 4000 characters.
    /// The user must be a participant in the conversation to send messages.
    /// </remarks>
    /// <response code="200">Message sent successfully</response>
    /// <response code="400">Invalid message content or conversation ID</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a participant in the conversation</response>
    /// <response code="404">Conversation not found</response>
    [HttpPost("send")]
    [ProducesResponseType(typeof(MessageDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            Logger.LogInformation("[Messages] User {UserId} sending message to conversation {ConversationId}", 
                currentUserId, request.ConversationId);

            var messageDto = await _messagingService.SendAsync(request, currentUserId, ct);
            
            Logger.LogInformation("[Messages] Message {MessageId} sent successfully", messageDto.MessageId);
            return Ok(messageDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "[Messages] Unauthorized attempt to send message to conversation {ConversationId}", 
                request.ConversationId);
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "[Messages] Invalid message request");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get messages from a conversation with pagination
    /// </summary>
    /// <param name="conversationId">ID of the conversation</param>
    /// <param name="page">Page number (minimum: 1, default: 1)</param>
    /// <param name="pageSize">Items per page (1-100, default: 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of messages ordered by timestamp ascending</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/v1/messages/conversations/123?page=1&amp;pageSize=20
    /// 
    /// Returns messages in chronological order (oldest first) for better conversation flow.
    /// The user must be a participant in the conversation to view messages.
    /// 
    /// Response includes pagination metadata:
    /// - total: Total number of messages
    /// - page: Current page number  
    /// - pageSize: Items per page
    /// - totalPages: Total number of pages
    /// - hasNextPage: Whether there are more pages
    /// - hasPreviousPage: Whether there are previous pages
    /// </remarks>
    /// <response code="200">Messages retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a participant in the conversation</response>
    /// <response code="404">Conversation not found</response>
    [HttpGet("conversations/{conversationId:int}")]
    [ProducesResponseType(typeof(PagedResult<MessageDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetConversationMessages(
        int conversationId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken ct = default)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(new { Error = "Page number must be at least 1." });
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { Error = "Page size must be between 1 and 100." });
            }

            var currentUserId = GetCurrentUserId();
            Logger.LogInformation("[Messages] User {UserId} requesting messages for conversation {ConversationId}, page {Page}", 
                currentUserId, conversationId, page);

            var result = await _messagingService.GetConversationAsync(conversationId, new PageSpec(page, pageSize), currentUserId, ct);
            
            Logger.LogInformation("[Messages] Retrieved {Count} messages for conversation {ConversationId}", 
                result.Items.Count(), conversationId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "[Messages] Unauthorized attempt to access conversation {ConversationId}", conversationId);
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "[Messages] Invalid request for conversation {ConversationId}", conversationId);
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's inbox with all conversations
    /// </summary>
    /// <param name="page">Page number (minimum: 1, default: 1)</param>
    /// <param name="pageSize">Items per page (1-100, default: 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of conversations ordered by last activity</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/v1/messages/inbox?page=1&amp;pageSize=20
    /// 
    /// Returns conversations ordered by most recent activity first.
    /// Each conversation includes:
    /// - conversationId: Unique conversation identifier
    /// - itemId: Associated item ID (if applicable)
    /// - lastMessage: Preview of the most recent message
    /// - lastUpdated: Timestamp of last activity
    /// - otherUserId: ID of the other participant
    /// - otherUserName: Name of the other participant
    /// - unreadCount: Number of unread messages
    /// 
    /// Response includes standard pagination metadata.
    /// </remarks>
    /// <response code="200">Inbox retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(PagedResult<ConversationListItem>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetInbox(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken ct = default)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(new { Error = "Page number must be at least 1." });
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { Error = "Page size must be between 1 and 100." });
            }

            var currentUserId = GetCurrentUserId();
            Logger.LogInformation("[Messages] User {UserId} requesting inbox, page {Page}", currentUserId, page);

            var result = await _messagingService.ListForUserAsync(currentUserId, new PageSpec(page, pageSize), ct);
            
            Logger.LogInformation("[Messages] Retrieved {Count} conversations for user {UserId}", 
                result.Items.Count(), currentUserId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "[Messages] Invalid inbox request");
            return BadRequest(new { Error = ex.Message });
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
            Logger.LogError("[Messages] User ID not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        
        return userId;
    }
}