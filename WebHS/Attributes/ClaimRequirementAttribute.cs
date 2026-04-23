using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace WebHS.Attributes
{
    public class ClaimRequirementAttribute : AuthorizeAttribute
    {
        public ClaimRequirementAttribute(string claimType, string claimValue)
        {
            Policy = $"{claimType}_{claimValue}";
        }
    }

    public class ClaimRequirement : IAuthorizationRequirement
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }

        public ClaimRequirement(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }

    public class ClaimRequirementHandler : AuthorizationHandler<ClaimRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimRequirement requirement)
        {
            var claim = context.User.FindFirst(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue);

            if (claim != null)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
