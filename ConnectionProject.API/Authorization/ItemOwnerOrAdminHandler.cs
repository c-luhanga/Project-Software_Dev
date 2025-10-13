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

        public ItemOwnerOrAdminHandler(IHttpContextAccessor httpContextAccessor, IItemRepository itemRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _itemRepository = itemRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ItemOwnerOrAdminRequirement requirement)
        {
            // Check if user has admin role first
            if (context.User.IsInRole("admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Get current HttpContext
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return; // Deny if no HttpContext
            }

            // Try to get item ID from route values (check both "id" and "itemId")
            var routeValues = httpContext.Request.RouteValues;
            var itemIdValue = routeValues["id"] ?? routeValues["itemId"];
            
            if (itemIdValue == null || !int.TryParse(itemIdValue.ToString(), out int itemId))
            {
                return; // Deny if no valid item ID found
            }

            // Get current user ID from the "sub" claim
            var userIdClaim = context.User.FindFirst("sub")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return; // Deny if no valid user ID found
            }

            try
            {
                // Query database to get seller and status
                var sellerAndStatus = await _itemRepository.GetSellerAndStatusAsync(itemId, CancellationToken.None);
                
                if (sellerAndStatus == null)
                {
                    return; // Deny if item not found
                }

                // Check if current user is the seller of the item
                if (sellerAndStatus.Value.SellerId == userId)
                {
                    context.Succeed(requirement);
                }
                // Otherwise, deny (do nothing)
            }
            catch
            {
                // Deny on any database errors
                return;
            }
        }
    }
}
