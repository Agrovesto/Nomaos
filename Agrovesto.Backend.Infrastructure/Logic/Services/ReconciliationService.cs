using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    
    public static class RoleNames
    {
        public const string Farmer = "Farmer";
        public const string Agent = "Agent";
        public const string Membership = "MemberShip";
        public const string Organization = "Organization";
    }

    public class UserRoleRow
    {public DateTimeOffset CreatedOn { get; set; }
        public string UserId { get; set; }
        public string RoleName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
         }
    public class NameParts
    {
        public string Full { get; set; }
        public string First { get; set; }
        public string Middle { get; set; }
        public string Last { get; set; }
    }

    public static class NameSplitter
    {
        /// <summary>
        /// Splits a full name into first/middle/last.
        /// Rules:
        /// - 1 word  -> First only
        /// - 2 words -> First, Last
        /// - 3 words -> First, Middle, Last
        /// - 4+ words -> First = first word, Last = last word, Middle = everything in between
        /// </summary>
        public static NameParts Split(string fullName)
        {
            var result = new NameParts { Full = fullName?.Trim() };

            if (string.IsNullOrWhiteSpace(fullName))
                return result;

            var parts = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            switch (parts.Length)
            {
                case 0:
                    break;
                case 1:
                    result.First = parts[0];
                    break;
                case 2:
                    result.First = parts[0];
                    result.Last = parts[1];
                    break;
                case 3:
                    result.First = parts[0];
                    result.Middle = parts[1];
                    result.Last = parts[2];
                    break;
                default:
                    result.First = parts[0];
                    result.Last = parts[^1];
                    result.Middle = string.Join(' ', parts[1..^1]);
                    break;
            }

            return result;
        }
    }
    public class UserRoleReconciliationResult
    {
        public bool IsDryRun { get; set; }
        public int FarmersCreated { get; set; }
        public int AgentsCreated { get; set; }
        public int MembershipsCreated { get; set; }
        public int OrganizationsCreated { get; set; }
        public List<Farmer> FarmersPreview { get; set; } = new();
        public List<Agent> AgentsPreview { get; set; } = new();
        public List<MemberShip> MembershipsPreview { get; set; } = new();
        public List<Organization> OrganizationsPreview { get; set; } = new();
        public List<string> SkippedOrIssues { get; set; } = new();
    }
    public interface IUserRoleReconciliationService
    {
        Task<UserRoleReconciliationResult> ReconcileAsync(bool dryRun = false, CancellationToken ct = default);
    }
    public class UserRoleReconciliationService : IUserRoleReconciliationService
    {
        private readonly DataContext _db;
        private readonly ILogger<UserRoleReconciliationService> _logger;

        // Fallback business code for records we can't otherwise determine one for.
        // Adjust to whatever makes sense for your domain (or make it a parameter).
        private const string DefaultBusinessCode = "AGROVESTO2025L";

        public UserRoleReconciliationService(DataContext db, ILogger<UserRoleReconciliationService> logger)
        {
            _db = db;
            _logger = logger;
        }
        /// <summary>
        /// Reconciles missing role-table records for all users.
        /// </summary>
        /// <param name="dryRun">If true, computes what would be created but does not save to the database.</param>

        public async Task<UserRoleReconciliationResult> ReconcileAsync(bool dryRun = false, CancellationToken ct = default)
        {
            var result = new UserRoleReconciliationResult { IsDryRun = dryRun};

            // 1. Pull every user/role assignment, with the identity fields we might need.
            var userRoleRows = await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                join u in _db.Users on ur.UserId equals u.Id
                select new UserRoleRow
                {
                    UserId = u.Id,
                    RoleName = r.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Name = u.Name,
                    CreatedOn=u.CreatedOn,
                    
                }
            ).ToListAsync(ct);

            if (userRoleRows.Count == 0)
            {
                _logger.LogInformation("No user-role assignments found.");
                return result;
            }

            // 2. Load existing UserIds per table (avoid per-row queries).
            var existingFarmerUserIds = await _db.Farmers.Select(f => f.UserId).ToHashSetAsync(ct);
            var existingAgentUserIds = await _db.Agents.Select(a => a.UserId).ToHashSetAsync(ct);
            var existingMembershipUserIds = await _db.MemberShips.Select(m => m.UserId).ToHashSetAsync(ct);
            var existingOrganizationUserIds = await _db.Organizations.Select(o => o.UserId).ToHashSetAsync(ct);

            var farmersToAdd = new List<Farmer>();
            var agentsToAdd = new List<Agent>();
            var membershipsToAdd = new List<MemberShip>();
            var organizationsToAdd = new List<Organization>();

            // Track within this run too, in case the same user/role appears twice
            // (defensive against duplicate AspNetUserRoles rows).
            var seenFarmer = new HashSet<string>();
            var seenAgent = new HashSet<string>();
            var seenMembership = new HashSet<string>();
            var seenOrganization = new HashSet<string>();

            foreach (var row in userRoleRows)
            {
                var nameParts = NameSplitter.Split(row.Name);
                switch (row.RoleName)
                {
                    case RoleNames.Farmer:
                        if (!existingFarmerUserIds.Contains(row.UserId) && seenFarmer.Add(row.UserId))
                        {
                            farmersToAdd.Add(new Farmer
                            {
                                UserId = row.UserId,
                                Email = row.Email,
                                PhoneNumber = row.PhoneNumber,
                                BelongToBusinessCode = DefaultBusinessCode,
                                OnBoardedOn = row.CreatedOn,
                                Level = FarmerLevel.Level1, // <-- set to your actual default enum value
                            Name = nameParts.Full,
                            });
                        }
                        break;

                    case RoleNames.Agent:
                        if (!existingAgentUserIds.Contains(row.UserId) && seenAgent.Add(row.UserId))
                        {
                            agentsToAdd.Add(new Agent
                            {
                                UserId = row.UserId,
                                Email = row.Email,
                                PhoneNumber = row.PhoneNumber,
                                BelongToBusinessCode = DefaultBusinessCode,
                                OnBoardedOn = row.CreatedOn,
                                IsActive = true,
                                TierLevel = Tier.Tier1, // <-- adjust to your default
                            Name = nameParts.Full,
                            });
                        }
                        break;

                    case RoleNames.Membership:
                        if (!existingMembershipUserIds.Contains(row.UserId) && seenMembership.Add(row.UserId))
                        {
                            membershipsToAdd.Add(new MemberShip
                            {
                                UserId = row.UserId,
                                Email = row.Email,
                                PhoneNumber = row.PhoneNumber,
                                BelongToBusinessCode = DefaultBusinessCode,
                                OnBoardedOn = row.CreatedOn,
                                IsActive = true,
                                TierLevel = Tier.Tier1,
                                MemberShipTypeId = 1, // <-- you'll likely need a real default FK here
                                 MembershipLeaderFirstName = nameParts.First,
                                MembershipLeaderMiddleName = nameParts.Middle,
                                MembershipLeaderLastName = nameParts.Last,
                                IsLeaderAFarmer=true,
                                Name=nameParts.Full,
                            });
                        }
                        break;

                    case RoleNames.Organization:
                        if (!existingOrganizationUserIds.Contains(row.UserId) && seenOrganization.Add(row.UserId))
                        {
                            organizationsToAdd.Add(new Organization
                            {
                                UserId = row.UserId,
                                ContactEmail = row.Email,
                                PhoneNumber = row.PhoneNumber,
                                BusinessCode = DefaultBusinessCode,
                                OnboardedOn = row.CreatedOn,
                                IsActive = true,
                                FullName = nameParts.Full,
                                FirstName = nameParts.First,
                                LastName = nameParts.Last,
                            });
                        }
                        break;

                    default:
                        result.SkippedOrIssues.Add($"UserId {row.UserId} has unrecognized role '{row.RoleName}', skipped.");
                        break;
                }
            }
            result.FarmersCreated = farmersToAdd.Count;
            result.AgentsCreated = agentsToAdd.Count;
            result.MembershipsCreated = membershipsToAdd.Count;
            result.OrganizationsCreated = organizationsToAdd.Count;

            if (dryRun)
            {
                // Return what WOULD be created, without touching the database.
                result.FarmersPreview = farmersToAdd;
                result.AgentsPreview = agentsToAdd;
                result.MembershipsPreview = membershipsToAdd;
                result.OrganizationsPreview = organizationsToAdd;

                _logger.LogInformation(
                    "[DRY RUN] Would create - Farmers: {F}, Agents: {A}, Memberships: {M}, Organizations: {O}. Issues: {I}",
                    result.FarmersCreated, result.AgentsCreated, result.MembershipsCreated, result.OrganizationsCreated,
                    result.SkippedOrIssues.Count);

                return result;
            }
            // 3. Persist everything in one transaction.
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (farmersToAdd.Count > 0) await _db.Farmers.AddRangeAsync(farmersToAdd, ct);
                if (agentsToAdd.Count > 0) await _db.Agents.AddRangeAsync(agentsToAdd, ct);
                if (membershipsToAdd.Count > 0) await _db.MemberShips.AddRangeAsync(membershipsToAdd, ct);
                if (organizationsToAdd.Count > 0) await _db.Organizations.AddRangeAsync(organizationsToAdd, ct);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            
            _logger.LogInformation(
                "Reconciliation complete. Farmers: {F}, Agents: {A}, Memberships: {M}, Organizations: {O}. Issues: {I}",
                result.FarmersCreated, result.AgentsCreated, result.MembershipsCreated, result.OrganizationsCreated,
                result.SkippedOrIssues.Count);

            return result;
        }
    }

    public static class QueryableExtensions
    {
        public static async Task<HashSet<T>> ToHashSetAsync<T>(
            this IQueryable<T> source,
            CancellationToken cancellationToken = default)
        {
            var list = await source.ToListAsync(cancellationToken);
            return list.ToHashSet();
        }
    }
}
