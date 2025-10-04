using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniShareProject.services.Interfaces;
using UniShareProject.services.Models;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Handles item management operations including CRUD operations and marketplace functionality
/// </summary>
[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(IItemService itemService, ILogger<ItemsController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new item for sale
    /// </summary>
    /// <remarks>
    /// Create a new item listing in the marketplace. Requires authentication.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/items
    ///     {
    ///        "title": "Calculus Textbook",
    ///        "description": "Like new condition calculus textbook for Math 151. No highlighting or writing inside.",
    ///        "categoryId": 1,
    ///        "price": 75.00,
    ///        "conditionId": 1
    ///     }
    /// 
    /// Category IDs: 1=Books, 2=Electronics, 3=Furniture, 4=Clothing, 5=Other
    /// Condition IDs: 1=Like New, 2=Good, 3=Fair, 4=Poor
    /// 
    /// The seller ID is automatically extracted from the JWT token.
    /// New items are created with "Active" status (statusId: 1).
    /// </remarks>
    /// <param name="request">Item creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created item with generated ID</returns>
    /// <response code="201">Item successfully created</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize] // Explicitly require authentication
    [ProducesResponseType(typeof(ItemDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Extract seller ID from JWT claim 'sub'
        var sellerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(sellerIdClaim) || !int.TryParse(sellerIdClaim, out int sellerId))
        {
            _logger.LogWarning("Unable to extract valid seller ID from JWT claims");
            throw new UnauthorizedAccessException("Invalid or missing user identification");
        }

        var itemDto = await _itemService.CreateAsync(request, sellerId, ct);
        
        _logger.LogInformation("Item created successfully with ID: {ItemId} by seller: {SellerId}", 
            itemDto.Id, sellerId);
        
        return CreatedAtAction(nameof(GetItem), new { id = itemDto.Id }, itemDto);
    }

    /// <summary>
    /// Get a specific item by ID
    /// </summary>
    /// <remarks>
    /// Retrieve detailed information about a specific item by its ID.
    /// This endpoint is publicly accessible and does not require authentication.
    /// 
    /// Returns item details including:
    /// - Basic information (title, description, price)
    /// - Category and condition details
    /// - Seller information
    /// - Current status and posting date
    /// </remarks>
    /// <param name="id">Item ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Item details or 404 if not found</returns>
    /// <response code="200">Item found and returned</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous] // Allow anonymous access to view items
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetItem(int id, CancellationToken ct)
    {
        var item = await _itemService.GetAsync(id, ct);
        
        if (item == null)
        {
            _logger.LogInformation("Item not found: {ItemId}", id);
            throw new KeyNotFoundException($"Item with ID {id} not found");
        }

        return Ok(item);
    }

    /// <summary>
    /// Search and filter items with pagination
    /// </summary>
    /// <remarks>
    /// Search for items using various filters and pagination.
    /// This endpoint is publicly accessible and does not require authentication.
    /// 
    /// Available filters:
    /// - categoryId: Filter by category (1=Books, 2=Electronics, 3=Furniture, 4=Clothing, 5=Other)
    /// - statusId: Filter by status (1=Active, 2=Pending, 3=Sold, 4=Withdrawn)
    /// - conditionId: Filter by condition (1=Like New, 2=Good, 3=Fair, 4=Poor)
    /// - q: Text search in title and description
    /// - page: Page number (default: 1)
    /// - pageSize: Items per page (default: 20, max: 100)
    /// 
    /// Example queries:
    /// - /api/items?categoryId=1&amp;q=calculus (Books containing "calculus")
    /// - /api/items?statusId=1&amp;page=2&amp;pageSize=10 (Active items, page 2)
    /// - /api/items?conditionId=1&amp;categoryId=2 (Like new electronics)
    /// </remarks>
    /// <param name="request">Search parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of items matching the search criteria</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid search parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [AllowAnonymous] // Allow anonymous access to search items
    [ProducesResponseType(typeof(PagedResultDto<ItemDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> SearchItems([FromQuery] SearchItemsRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _itemService.SearchAsync(request, ct);
        
        _logger.LogInformation("Items search completed. Page: {Page}, PageSize: {PageSize}, Total: {Total}", 
            request.Page, request.PageSize, result.Total);
        
        return Ok(result);
    }

    /// <summary>
    /// Request to purchase an item
    /// </summary>
    /// <remarks>
    /// Submit a purchase request for an available item. Requires authentication.
    /// 
    /// This action:
    /// - Changes the item status from "Active" (1) to "Pending" (2)
    /// - Prevents other users from purchasing the same item
    /// - Records the buyer information from the JWT token
    /// 
    /// The item must be in "Active" status to be purchased.
    /// If the item is already pending, sold, or withdrawn, the request will fail with 409 Conflict.
    /// 
    /// This is the primary "buy" action in the marketplace MVP.
    /// </remarks>
    /// <param name="id">Item ID to purchase</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated item with Pending status</returns>
    /// <response code="200">Purchase request successful, item now pending</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Item not found</response>
    /// <response code="409">Item not available for purchase (already pending/sold/withdrawn)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id:int}/request-purchase")]
    [Authorize] // Explicitly require authentication
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> RequestPurchase(int id, CancellationToken ct)
    {
        // Extract buyer ID from JWT claim 'sub'
        var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(buyerIdClaim) || !int.TryParse(buyerIdClaim, out int buyerId))
        {
            _logger.LogWarning("Unable to extract valid buyer ID from JWT claims");
            throw new UnauthorizedAccessException("Invalid or missing user identification");
        }

        var updatedItem = await _itemService.RequestPurchaseAsync(id, buyerId, ct);
        
        _logger.LogInformation("Purchase requested successfully. ItemId: {ItemId}, Buyer: {BuyerId}", 
            id, buyerId);
        
        return Ok(updatedItem);
    }

    /// <summary>
    /// Update item status (Admin/Seller operation)
    /// </summary>
    /// <remarks>
    /// Update the status of an item. Requires authentication.
    /// 
    /// Status values:
    /// - 1: Active (available for purchase)
    /// - 2: Pending (purchase requested)
    /// - 3: Sold (transaction completed)
    /// - 4: Withdrawn (removed from sale)
    /// 
    /// This endpoint is primarily for administrative purposes or seller management.
    /// Regular users should use the specific action endpoints like /request-purchase.
    /// 
    /// Future enhancement: Add authorization to restrict to item owner or admins.
    /// </remarks>
    /// <param name="id">Item ID</param>
    /// <param name="statusId">New status ID (1=Active, 2=Pending, 3=Sold, 4=Withdrawn)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated item</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="400">Invalid status ID</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Not authorized to update this item</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id:int}/status")]
    [Authorize] // Explicitly require authentication
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UpdateItemStatus(int id, [FromQuery] byte statusId, CancellationToken ct)
    {
        // Extract actor ID from JWT claim 'sub'
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(actorIdClaim) || !int.TryParse(actorIdClaim, out int actorId))
        {
            _logger.LogWarning("Unable to extract valid actor ID from JWT claims");
            throw new UnauthorizedAccessException("Invalid or missing user identification");
        }

        var updatedItem = await _itemService.UpdateStatusAsync(id, statusId, actorId, ct);
        
        _logger.LogInformation("Item status updated successfully. ItemId: {ItemId}, NewStatus: {StatusId}, Actor: {ActorId}", 
            id, statusId, actorId);
        
        return Ok(updatedItem);
    }
}