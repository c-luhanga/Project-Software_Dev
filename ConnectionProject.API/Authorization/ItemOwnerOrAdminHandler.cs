using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UniShareProject.Repository.Repositories;

namespace UniShareProject.API.Authorization
{
    public class ItemOwnerOrAdminRequirement : IAuthorizationRequirement
    {
        // Empty requirement, logic handled in handler
    }

    public class ItemOwnerOrAdminHandler : AuthorizationHandler<ItemOwnerOrAdminRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ItemOwnerOrAdminHandler> _logger;

        public ItemOwnerOrAdminHandler(IHttpContextAccessor httpContextAccessor, IItemRepository itemRepository, ILogger<ItemOwnerOrAdminHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _itemRepository = itemRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ItemOwnerOrAdminRequirement requirement)
        {
            _logger.LogInformation("[ItemOwnerOrAdmin] Starting authorization check");

            // Debug: Log all available claims
            var claims = context.User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            _logger.LogInformation("[ItemOwnerOrAdmin] Available claims: {Claims}", string.Join(", ", claims));

            // Check if user has admin role first
            if (context.User.IsInRole("admin"))
            {
                _logger.LogInformation("[ItemOwnerOrAdmin] User has admin role - AUTHORIZED");
                context.Succeed(requirement);
                return;
            }

            // Get current HttpContext
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("[ItemOwnerOrAdmin] No HttpContext available - DENIED");
                return; // Deny if no HttpContext
            }

            // Try to get item ID from route values (check both "id" and "itemId")
            var routeValues = httpContext.Request.RouteValues;
            var itemIdValue = routeValues["id"] ?? routeValues["itemId"];
            
            _logger.LogInformation("[ItemOwnerOrAdmin] Route values: {RouteValues}", string.Join(", ", routeValues.Select(kv => $"{kv.Key}={kv.Value}")));
            
            if (itemIdValue == null || !int.TryParse(itemIdValue.ToString(), out int itemId))
            {
                _logger.LogWarning("[ItemOwnerOrAdmin] No valid item ID found in route values - DENIED");
                return; // Deny if no valid item ID found
            }

            _logger.LogInformation("[ItemOwnerOrAdmin] Found item ID: {ItemId}", itemId);

            // Get current user ID from various possible claim types
            string? userIdClaim = context.User.FindFirst("sub")?.Value
                               ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? context.User.FindFirst("nameid")?.Value
                               ?? context.User.FindFirst("unique_name")?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("[ItemOwnerOrAdmin] No valid user ID found in any claim type - DENIED. Tried: sub, {NameIdentifier}, nameid, unique_name", ClaimTypes.NameIdentifier);
                return; // Deny if no valid user ID found
            }

            _logger.LogInformation("[ItemOwnerOrAdmin] Found user ID: {UserId}", userId);

            try
            {
                // Query database to get seller and status
                var sellerAndStatus = await _itemRepository.GetSellerAndStatusAsync(itemId, CancellationToken.None);
                
                if (sellerAndStatus == null)
                {
                    _logger.LogWarning("[ItemOwnerOrAdmin] Item {ItemId} not found in database - DENIED", itemId);
                    return; // Deny if item not found
                }

                _logger.LogInformation("[ItemOwnerOrAdmin] Item {ItemId} found - SellerId: {SellerId}, StatusId: {StatusId}", itemId, sellerAndStatus.Value.SellerId, sellerAndStatus.Value.StatusId);

                // Check if current user is the seller of the item
                if (sellerAndStatus.Value.SellerId == userId)
                {
                    _logger.LogInformation("[ItemOwnerOrAdmin] User {UserId} is owner of item {ItemId} - AUTHORIZED", userId, itemId);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("[ItemOwnerOrAdmin] User {UserId} is NOT owner of item {ItemId} (owner is {SellerId}) - DENIED", userId, itemId, sellerAndStatus.Value.SellerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ItemOwnerOrAdmin] Database error while checking item ownership for item {ItemId} and user {UserId} - DENIED", itemId, userId);
                return;
            }
        }
    }
}
