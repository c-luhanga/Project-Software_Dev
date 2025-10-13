using Microsoft.AspNetCore.Authorization;

namespace UniShareProject.API.Authorization
{
    public class ItemOwnerOrAdminRequirement : IAuthorizationRequirement
    {
        // Empty requirement, logic handled in handler
    }

    public class ItemOwnerOrAdminHandler : AuthorizationHandler<ItemOwnerOrAdminRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ItemOwnerOrAdminRequirement requirement)
        {
            // Example logic: allow if user is admin, or item owner (customize as needed)
            if (context.User.IsInRole("admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            // Item ownership logic should be implemented in the resource-based handler in your controller
            // For now, just fail if not admin
            return Task.CompletedTask;
        }
    }
}
