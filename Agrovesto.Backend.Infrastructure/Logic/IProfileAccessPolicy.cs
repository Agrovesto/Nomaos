using Agrovesto.Backend.Core.V2.Onboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic
{
    public interface IProfileAccessPolicy
    {
        Task<bool> CanEditAsync(ClaimsPrincipal caller, string targetUserId, ProfileRole role, string businessCode);
    }

    public sealed class DefaultProfileAccessPolicy : IProfileAccessPolicy
    {
        public const string AgentRole = "Agent";
        public const string AdminRole = "Admin";
        public const string Organization = "Organization";
        public const string Membership = "MemberShip";

        public Task<bool> CanEditAsync(ClaimsPrincipal caller, string targetUserId, ProfileRole role, string businessCode)
        {
            var callerId = caller.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(callerId)) return Task.FromResult(false);

            // Self-edit: only for a role the caller actually holds
            // (stops a farmer from creating an Agent or Cooperative profile for themselves).
            if (string.Equals(callerId, targetUserId, StringComparison.Ordinal))
                return Task.FromResult(caller.IsInRole(role.ToString()));

            // On behalf of someone else: staff or field agents only.
            // TIGHTEN HERE: for agents, also verify the target is one of their farmers/cooperatives
            // (e.g. a FarmerAgents lookup) and that businessCode is a business the agent belongs to.
            return Task.FromResult(caller.IsInRole(AdminRole) || caller.IsInRole(AgentRole)|| caller.IsInRole(Membership)|| caller.IsInRole(Organization));
        }
    }
}
