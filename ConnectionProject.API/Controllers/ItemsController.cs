using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShareProject.services.Interfaces;
using UniShareProject.services.Models;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using ConnectionProject.API.Controllers.Base;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Handles item management operations including CRUD operations and marketplace functionality
/// </summary>
[ApiController]
[Route("api/items")]
public class ItemsController : BaseApiController
{
    private readonly IItemService _itemService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IItemImageRepository _itemImageRepository;

    public ItemsController(IItemService itemService, IFileUploadService fileUploadService, IItemImageRepository itemImageRepository, ILogger<ItemsController> logger) : base(logger)
    {
        _itemService = itemService;
        _fileUploadService = fileUploadService;
        _itemImageRepository = itemImageRepository;
    }

    /// <summary>
    /// Get all items posted by the current logged-in user
    /// </summary>
    /// <remarks>
    /// Retrieves all items that the currently authenticated user has posted to the marketplace.
    /// This includes items in all statuses (Active, Pending, Sold, Withdrawn).
    /// 
    /// Sample request:
    /// 
    ///     GET /api/items/my-items
    ///     Authorization: Bearer {token}
    /// 
    /// The user ID is automatically extracted from the JWT token.
    /// This endpoint is useful for:
    /// - Viewing your own listings
    /// - Managing your posted items
    /// - Checking the status of your sales
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of items posted by the current user</returns>
    /// <response code="200">Items retrieved successfully (returns empty array if no items found)</response>
    /// <response code="401">Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-items")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<Item>), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetMyItems(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        
        Logger.LogInformation("User {UserId} retrieving their posted items", userId);
        
        var items = await _itemService.GetByUserIdAsync(userId);
        
        var itemList = items.ToList();
        Logger.LogInformation("User {UserId} has {ItemCount} posted items", userId, itemList.Count);
        
        return Ok(itemList);
    }

    /// <summary>
    /// Create a new item for sale
    /// </summary>
    /// <remarks>
    /// Create a new item listing in the marketplace. Requires authentication with @principia.edu email.
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
    /// <response code="403">Must use @principia.edu email</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Policy = "PrincipiaEmail")]
    [ProducesResponseType(typeof(ItemDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
            return validationResult;

        var sellerId = GetCurrentUserId();

        var itemDto = await _itemService.CreateAsync(request, sellerId, ct);
        
        Logger.LogInformation("Item created successfully with ID: {ItemId} by seller: {SellerId}", 
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
            Logger.LogInformation("Item not found: {ItemId}", id);
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
        var validationResult = ValidateModelState();
        if (validationResult != null)
            return validationResult;

        var result = await _itemService.SearchAsync(request, ct);
        
        Logger.LogInformation("Items search completed. Page: {Page}, PageSize: {PageSize}, Total: {Total}", 
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
        var buyerId = GetCurrentUserId();

        var updatedItem = await _itemService.RequestPurchaseAsync(id, buyerId, ct);
        
        Logger.LogInformation("Purchase requested successfully. ItemId: {ItemId}, Buyer: {BuyerId}", 
            id, buyerId);
        
        return Ok(updatedItem);
    }

    /// <summary>
    /// Update item status (Admin/Seller operation)
    /// </summary>
    /// <remarks>
    /// Update the status of an item. Requires authentication and authorization (item owner or admin).
    /// 
    /// Status values:
    /// - 1: Active (available for purchase)
    /// - 2: Pending (purchase requested)
    /// - 3: Sold (transaction completed)
    /// - 4: Withdrawn (removed from sale)
    /// 
    /// This endpoint is restricted to item owners and administrators.
    /// Regular users should use the specific action endpoints like /request-purchase.
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
    [Authorize]
    [Authorize(Policy = "ItemOwnerOrAdmin")]
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UpdateItemStatus(int id, [FromQuery] byte statusId, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();

        var updatedItem = await _itemService.UpdateStatusAsync(id, statusId, actorId, ct);
        
        Logger.LogInformation("Item status updated successfully. ItemId: {ItemId}, NewStatus: {StatusId}, Actor: {ActorId}", 
            id, statusId, actorId);
        
        return Ok(updatedItem);
    }

    /// <summary>
    /// Upload image files to an existing item
    /// </summary>
    /// <remarks>
    /// Upload 1-4 actual image files to an existing item. Requires authentication and authorization (item owner or admin).
    /// This is the NEW file upload endpoint that handles actual file uploads, not just URLs.
    /// 
    /// This action:
    /// - Validates that the authenticated user is the seller of the item or an admin
    /// - Validates uploaded files (type, size, format)
    /// - Stores files securely on the server or cloud storage
    /// - Automatically generates public URLs for the uploaded images
    /// - Ensures the total number of images (existing + new) does not exceed 4
    /// - Returns all image URLs for the item after uploading
    /// 
    /// Sample request (multipart/form-data):
    /// 
    ///     POST /api/items/123/upload-images
    ///     Content-Type: multipart/form-data
    ///     Authorization: Bearer {token}
    ///     
    ///     Form Data:
    ///     files: [image1.jpg, image2.png, image3.webp]
    /// 
    /// Supported file types: JPG, JPEG, PNG, GIF, WebP, BMP
    /// Maximum file size: 5MB per file
    /// Maximum total images per item: 4
    /// </remarks>
    /// <param name="id">Item ID to upload images to</param>
    /// <param name="files">Image files to upload (1-4 files)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>All image URLs for the item after uploading</returns>
    /// <response code="200">Images uploaded successfully</response>
    /// <response code="400">Invalid files, file validation errors, or no files provided</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Not authorized to upload images to this item</response>
    /// <response code="404">Item not found</response>
    /// <response code="409">Would exceed maximum of 4 images per item</response>
    /// <response code="413">File too large</response>
    /// <response code="415">Unsupported file type</response>
    /// <response code="500">Internal server error or upload failure</response>
    [HttpPost("{id:int}/upload-images")]
    [Authorize]
    [Authorize(Policy = "ItemOwnerOrAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 413)]
    [ProducesResponseType(typeof(object), 415)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UploadItemImages(int id, [FromForm] IList<IFormFile> files, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();

        // Validate basic input
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { error = "At least one image file is required" });
        }

        if (files.Count > 4)
        {
            return BadRequest(new { error = "Maximum 4 files allowed per upload" });
        }

        try
        {
            Logger.LogInformation("Starting file upload for item {ItemId} by user {ActorId}. File count: {FileCount}", 
                id, actorId, files.Count);

            // Validate all files before uploading any
            foreach (var file in files)
            {
                if (!_fileUploadService.IsValidImageFile(file))
                {
                    var maxSizeMB = _fileUploadService.GetMaxFileSizeBytes() / (1024.0 * 1024.0);
                    var supportedTypes = string.Join(", ", _fileUploadService.GetSupportedExtensions());
                    
                    return BadRequest(new { 
                        error = $"Invalid file: {file.FileName}",
                        details = $"File must be an image, max {maxSizeMB:F1}MB, supported types: {supportedTypes}"
                    });
                }
            }

            // Check current item exists and user has permission (this will be validated by the service)
            var item = await _itemService.GetAsync(id, ct);
            if (item == null)
            {
                return NotFound(new { error = $"Item with ID {id} not found" });
            }

            // Check image count limit using the repository to get current count
            var currentImageCount = await _itemImageRepository.CountByItemAsync(id, ct);
            var totalAfterUpload = currentImageCount + files.Count;
            
            if (totalAfterUpload > 4)
            {
                return Conflict(new { 
                    error = $"Cannot upload {files.Count} files. Item already has {currentImageCount} images. Maximum allowed is 4 total.",
                    currentImageCount,
                    requestedUpload = files.Count,
                    maximumAllowed = 4
                });
            }

            // Upload files and get URLs
            var uploadedUrls = await _fileUploadService.UploadImagesAsync(files, id, ct);

            // Create request to add URLs to database
            var addImagesRequest = new AddItemImagesRequest(uploadedUrls.ToList());
            var allImageUrls = await _itemService.AddImagesAsync(id, actorId, addImagesRequest, ct);

            Logger.LogInformation("Successfully uploaded {UploadCount} files for item {ItemId} by user {ActorId}. Total images: {TotalCount}", 
                uploadedUrls.Count, id, actorId, allImageUrls.Count);

            return Ok(allImageUrls);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("File validation error for item {ItemId}: {Error}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning("Unauthorized upload attempt for item {ItemId} by user {ActorId}: {Error}", id, actorId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Upload operation failed for item {ItemId} by user {ActorId}", id, actorId);
            return StatusCode(500, new { error = "Upload failed", details = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during upload for item {ItemId} by user {ActorId}", id, actorId);
            return StatusCode(500, new { error = "An unexpected error occurred during upload" });
        }
    }

    /// <summary>
    /// Add images to an existing item
    /// </summary>
    /// <remarks>
    /// Add 1-4 image URLs to an existing item. Requires authentication and authorization (item owner or admin).
    /// 
    /// This action:
    /// - Validates that the authenticated user is the seller of the item or an admin
    /// - Ensures the total number of images (existing + new) does not exceed 4
    /// - Adds the provided image URLs to the item
    /// - Returns all image URLs for the item after adding
    /// 
    /// Sample request:
    /// 
    ///     POST /api/items/123/images
    ///     {
    ///        "imageUrls": [
    ///          "https://example.com/image1.jpg",
    ///          "https://example.com/image2.jpg"
    ///        ]
    ///     }
    /// 
    /// Each URL must be a valid absolute HTTP/HTTPS URL.
    /// The maximum total number of images per item is 4.
    /// </remarks>
    /// <param name="id">Item ID to add images to</param>
    /// <param name="request">Request containing image URLs to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>All image URLs for the item after adding</returns>
    /// <response code="200">Images added successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Not authorized to add images to this item</response>
    /// <response code="404">Item not found</response>
    /// <response code="409">Would exceed maximum of 4 images per item</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id:int}/images")]
    [Authorize]
    [Authorize(Policy = "ItemOwnerOrAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> AddItemImages(int id, [FromBody] AddItemImagesRequest request, CancellationToken ct)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
            return validationResult;

        var actorId = GetCurrentUserId();

        var imageUrls = await _itemService.AddImagesAsync(id, actorId, request, ct);
        
        Logger.LogInformation("Images added successfully to item {ItemId} by user {ActorId}. Total images: {ImageCount}", 
            id, actorId, imageUrls.Count);
        
        return Ok(imageUrls);
    }

    /// <summary>
    /// Mark an item as sold
    /// </summary>
    /// <remarks>
    /// Mark a pending item as sold. Requires authentication and authorization (item owner or admin).
    /// 
    /// This action:
    /// - Validates that the authenticated user is the seller of the item or an admin
    /// - Ensures the item is currently in "Pending" status (2)
    /// - Updates the item status to "Sold" (3)
    /// - Returns the updated item details
    /// 
    /// The item must be in "Pending" status to be marked as sold.
    /// This typically happens after a purchase request has been made and the transaction is completed.
    /// </remarks>
    /// <param name="id">Item ID to mark as sold</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated item with Sold status</returns>
    /// <response code="200">Item marked as sold successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Not authorized to mark this item as sold</response>
    /// <response code="404">Item not found</response>
    /// <response code="409">Item cannot be marked as sold (not in Pending status)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id:int}/mark-sold")]
    [Authorize]
    [Authorize(Policy = "ItemOwnerOrAdmin")]
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> MarkItemSold(int id, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();

        var updatedItem = await _itemService.MarkSoldAsync(id, actorId, ct);
        
        Logger.LogInformation("Item {ItemId} marked as sold by user {ActorId}", id, actorId);
        
        return Ok(updatedItem);
    }
}